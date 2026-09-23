"""Browser acceptance run. Set HEADFUL=1 for a visible Chromium window."""
import os
import subprocess
import sys
import tempfile
import time
from pathlib import Path
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
    from workorders.models import Employee
    call_command("migrate", verbosity=0)
    Employee.objects.create(username="acceptance-tech", first_name="Casey", last_name="Tech")
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
        page.get_by_role("link", name="New work order").click()
        page.get_by_label("Title").fill("Acceptance: repair sink")
        page.get_by_label("Room number").fill("204")
        page.get_by_label("Creator").select_option(label="Casey Tech")
        page.get_by_role("button", name="Create work order").click()
        page.get_by_role("heading", name="WO-000001 · Acceptance: repair sink").wait_for()
        page.get_by_label("New status").select_option("Assigned")
        page.get_by_role("button", name="Update status").click()
        page.get_by_text("Work order WO-000001 moved to Assigned.").wait_for()
        page.screenshot(path=str(ARTIFACTS / "acceptance-result.png"), full_page=True)
        context.close()
        browser.close()
    print(f"Acceptance passed; screenshot and browser video saved under {ARTIFACTS}")
finally:
    server.terminate()
    server.wait(timeout=10)
    try: os.unlink(dbfile.name)
    except OSError: pass
