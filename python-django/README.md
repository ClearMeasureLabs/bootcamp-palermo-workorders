# Django work order prototype

This is an isolated Python/Django implementation track inside the original repository. It currently implements the primary work-order list, creation, search, detail, status lifecycle and history, employee references, and health endpoint. It is a prototype; see `../docs/django-rewrite-catalog.md` for the source-system inventory and explicit parity gaps.

## Run

```bash
cd python-django
python3 -m venv .venv
. .venv/bin/activate
pip install -r requirements.txt
python manage.py migrate
python manage.py runserver
```

Open http://127.0.0.1:8000/. SQLite is the default local database. `DJANGO_DB_PATH` selects a different SQLite file. Use PostgreSQL in a production deployment via Django's native PostgreSQL backend and `psycopg`; SQL Server and the original Azure deployment packaging have not been ported yet.

## Checks and full-system acceptance

`./scripts/private_build.sh` creates an isolated virtual environment, installs dependencies, deletes and recreates a clean build database, applies migrations, and runs Django tests. Add `RUN_ACCEPTANCE=1` to install Chromium and run browser acceptance. `pwsh -File ./PrivateBuild.ps1 -Acceptance` provides the clean build entry point expected by Windows Bootcamp users; `pwsh -File ./AcceptanceTests.ps1 -Headful` runs the browser suite separately. Acceptance tests save a screenshot and Playwright browser video under `artifacts/`; set `HEADFUL=1` to show Chromium where a graphical desktop is available.

This track maps xUnit to Django's `TestCase`, EF Core/DbUp to Django ORM migrations, Blazor to Django templates, and Playwright .NET to Playwright Python. The initial workflow is SQLite-first and does not yet replace the original release/deploy system.
