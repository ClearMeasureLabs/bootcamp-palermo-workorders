# Django work order prototype

This is an isolated Python/Django implementation track inside the original repository. It currently implements employee-picker sessions, role-aware create access, creator/assignee transition authorization, metadata-only attachments, the primary work-order list, creation, search, detail, status lifecycle and history, health endpoint, read-time due-date urgency, overdue filtering, and the 900-character room-number limit. It is a prototype; see `../docs/django-rewrite-catalog.md` for the source-system inventory and explicit parity gaps, and `../docs/django-source-inventory.csv` for the complete path-by-path repository catalog.

## Run

```bash
cd python-django
python3 -m venv .venv
. .venv/bin/activate
pip install -r requirements.txt
export DJANGO_SECRET_KEY="$(python -c 'import secrets; print(secrets.token_urlsafe(48))')"
export DJANGO_DEBUG=1
export DJANGO_ENABLE_DEMO_LOGIN=1
python manage.py migrate
python manage.py runserver
```

Open http://127.0.0.1:8000/. SQLite is the default local database. `DJANGO_DB_PATH` selects a different SQLite file. Use PostgreSQL in a production deployment via Django's native PostgreSQL backend and `psycopg`; SQL Server and the original Azure deployment packaging have not been ported yet.

Set a secret key before running management commands or the server; settings fail closed when it is missing. `DJANGO_DEBUG` defaults to `0`; enable it explicitly only during local development.

The employee-picker login mirrors the source demo flow and does not verify a password. It is disabled by default; set `DJANGO_ENABLE_DEMO_LOGIN=1` only for local/test environments. A production identity provider is not implemented.

## Checks and full-system acceptance

`./scripts/private_build.sh` creates an isolated virtual environment, installs dependencies, deletes and recreates a clean build database, applies migrations, and runs Django tests. Add `RUN_ACCEPTANCE=1` to install Chromium and run browser acceptance. `pwsh -File ./PrivateBuild.ps1 -Acceptance` provides the clean build entry point expected by Windows Bootcamp users; `pwsh -File ./AcceptanceTests.ps1 -Headful` runs the browser suite separately. Acceptance tests save a screenshot and Playwright browser video under `artifacts/`; set `HEADFUL=1` to show Chromium where a graphical desktop is available.

This track maps xUnit to Django's `TestCase`, EF Core/DbUp to Django ORM migrations, Blazor to Django templates, and Playwright .NET to Playwright Python. The initial workflow is SQLite-first and does not yet replace the original release/deploy system.
