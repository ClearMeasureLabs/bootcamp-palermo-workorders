"""Browser acceptance run. Set HEADFUL=1 for a visible Chromium window."""
import os
import subprocess
import sys
import tempfile
import time
from datetime import timedelta
from pathlib import Path
from zoneinfo import ZoneInfo
from django.utils import timezone
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parent
ARTIFACTS = Path(os.getenv("ACCEPTANCE_ARTIFACTS", ROOT / "artifacts"))
ARTIFACTS.mkdir(parents=True, exist_ok=True)
dbfile = tempfile.NamedTemporaryFile(prefix="workorders-acceptance-", suffix=".sqlite3", delete=False)
dbfile.close()
env = os.environ.copy()
env["DJANGO_DB_PATH"] = dbfile.name
env["DJANGO_ALLOWED_HOSTS"] = "127.0.0.1,localhost"
port = os.getenv("ACCEPTANCE_PORT", "8765")
server = subprocess.Popen([sys.executable, "manage.py", "runserver", f"127.0.0.1:{port}", "--noreload"], cwd=ROOT, env=env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
try:
    import django
    os.environ["DJANGO_SETTINGS_MODULE"] = "config.settings"
    os.environ["DJANGO_DB_PATH"] = dbfile.name
    django.setup()
    from django.core.management import call_command
    from workorders.models import Employee, Role, WorkOrder
    call_command("migrate", verbosity=0)
    employee = Employee.objects.create(username="acceptance-tech", first_name="Casey", last_name="Tech")
    employee.roles.add(Role.objects.create(name="Acceptance Creator", can_create_work_order=True))
    today = timezone.localdate(timezone.now(), ZoneInfo("America/Chicago"))
    WorkOrder.objects.create(title="Acceptance: open overdue", due_date=today - timedelta(days=2))
    WorkOrder.objects.create(title="Acceptance: closed overdue", due_date=today - timedelta(days=2), status=WorkOrder.Status.COMPLETE)
    for _ in range(60):
        if server.poll() is not None:
            raise RuntimeError("Django test server exited before becoming ready")
        try:
            import urllib.request
            urllib.request.urlopen(f"http://127.0.0.1:{port}/health/", timeout=1)
            break
        except Exception:
            time.sleep(.5)
    else:
        raise RuntimeError("Django test server did not become ready")
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=os.getenv("HEADFUL") != "1")
        context = browser.new_context(record_video_dir=str(ARTIFACTS / "video"), viewport={"width": 1365, "height": 900})
        page = context.new_page()
        page.goto(f"http://127.0.0.1:{port}/")
        page.get_by_role("heading", name="Work orders").wait_for()
        page.get_by_role("link", name="Log in").click()
        page.get_by_label("Select Church Member").select_option("acceptance-tech")
        page.get_by_role("button", name="Enter the Portal").click()
        page.get_by_text("CASEY TECH").wait_for()
        page.reload()
        page.get_by_text("CASEY TECH").wait_for()
        page.get_by_role("link", name="New work order").click()
        page.get_by_label("Title").fill("Acceptance: repair sink")
        page.get_by_label("Room number").fill("R" * 900)
        page.get_by_label("Due date").fill(today.isoformat())
        page.get_by_role("button", name="Create work order").click()
        heading = page.get_by_role("heading", name="Acceptance: repair sink")
        heading.wait_for()
        number = heading.inner_text().split(" · ", 1)[0]
        page.get_by_label("New status").select_option("Assigned")
        page.get_by_role("button", name="Update status").click()
        page.get_by_text(f"Work order {number} moved to Assigned.").wait_for()
        page.goto(f"http://127.0.0.1:{port}/")
        due_cell = page.get_by_test_id(f"due-date-{number}")
        assert "due-date-today" in (due_cell.get_attribute("class") or "")
        assert page.get_by_test_id(f"urgency-badge-{number}").inner_text() == "Due Today"
        page.get_by_label("Show overdue only").check()
        page.get_by_role("button", name="Search").click()
        page.get_by_text("Acceptance: open overdue").wait_for()
        assert page.get_by_text("Acceptance: closed overdue").count() == 0
        assert page.get_by_text("Acceptance: repair sink").count() == 0
        page.goto(f"http://127.0.0.1:{port}/work-orders/new/")
        page.get_by_label("Title").fill("Acceptance: invalid room")
        room = page.get_by_label("Room number")
        room.evaluate("element => element.removeAttribute('maxlength')")
        room.fill("X" * 901)
        page.get_by_role("button", name="Create work order").click()
        room_error = page.locator("#id_room_number_error")
        room_error.wait_for()
        assert "Ensure this value has at most 900 characters" in room_error.inner_text()
        page.get_by_role("button", name="Log out").click()
        page.get_by_role("heading", name="Log in").wait_for()
        page.goto(f"http://127.0.0.1:{port}/")
        assert page.get_by_role("link", name="New work order").count() == 0
        page.screenshot(path=str(ARTIFACTS / "acceptance-result.png"), full_page=True)
        context.close()
        browser.close()
    print(f"Acceptance passed; screenshot and browser video saved under {ARTIFACTS}")
finally:
    server.terminate()
    server.wait(timeout=10)
    try: os.unlink(dbfile.name)
    except OSError: pass
