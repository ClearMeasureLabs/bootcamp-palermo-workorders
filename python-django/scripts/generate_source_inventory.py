"""Generate a complete path-by-path source repository porting inventory."""

import csv
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "docs" / "django-source-inventory.csv"


def classify(path: str) -> tuple[str, str, str, str]:
    parts = Path(path).parts
    suffix = Path(path).suffix.lower() or "[no extension]"
    if len(parts) >= 2 and parts[0] == "src":
        if len(parts) >= 3 and parts[1] == "UI":
            area = f"src/UI/{parts[2]}"
            if parts[2] in {"Client", "Shared"}:
                destination, status = "python-django/workorders/templates/workorders/; forms.py; views.py", "partial; continue porting"
            elif parts[2] == "Api":
                destination, status = "Django views or Django REST Framework API (not implemented)", "deferred"
            else:
                destination, status = "python-django/config/; views.py; middleware or protocol-specific service", "partial; continue porting"
            return suffix, area, destination, status
        area = f"src/{parts[1]}"
        if parts[1] == "Core":
            destination, status = "python-django/workorders/models.py; services.py; forms.py", "partial; continue porting"
        elif parts[1] == "DataAccess":
            destination, status = "python-django/workorders/models.py; views.py; services.py", "partial; continue porting"
        elif parts[1] in {"UnitTests", "IntegrationTests", "AcceptanceTests"}:
            destination, status = "python-django/workorders/tests.py; python-django/acceptance_tests.py", "partial; continue porting"
        elif parts[1] == "Database":
            destination, status = "python-django/workorders/migrations/; config/settings.py", "partial; continue porting"
        elif parts[1] in {"UI.Client", "UI.Shared"}:
            destination, status = "python-django/workorders/templates/workorders/; forms.py; views.py", "partial; continue porting"
        elif parts[1] == "UI.Api":
            destination, status = "Django views or Django REST Framework API (not implemented)", "deferred"
        else:
            destination, status = "No Python counterpart yet; see docs/django-rewrite-catalog.md", "deferred"
        return suffix, area, destination, status
    if parts[0] == "feature-proposals":
        return suffix, "future proposals", "No current runtime mapping; assess proposal acceptance criteria individually", "deferred"
    if parts[0] == "python-django":
        return suffix, "Django rewrite", path, "implemented or rewrite tooling"
    if parts[0] in {"docs", "arch", "labs", "openspec", "video"}:
        return suffix, parts[0], "Documentation, experiments, or media; retain in original repository", "not a shipped application feature"
    if parts[0] in {".github", ".claude", ".cursor", ".squad", ".vscode", ".bob"}:
        return suffix, "repository tooling", "Retain existing repository tooling; Python CI/build coverage is tracked separately", "deferred or shared tooling"
    return suffix, "root repository", "Retain shared project metadata and developer tooling", "deferred or shared tooling"


def main() -> None:
    listed = subprocess.check_output(
        ["git", "ls-files", "--cached", "--others", "--exclude-standard", "-z"], cwd=ROOT
    )
    paths = {entry.decode() for entry in listed.split(b"\0") if entry}
    paths.add("docs/django-source-inventory.csv")
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT.open("w", newline="", encoding="utf-8") as target:
        writer = csv.writer(target)
        writer.writerow(["repository_path", "file_type", "source_area", "intended_python_destination", "port_status"])
        for path in sorted(paths):
            file_type, area, destination, status = classify(path)
            writer.writerow([path, file_type, area, destination, status])
    print(f"Wrote {len(paths)} repository paths to {OUTPUT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
