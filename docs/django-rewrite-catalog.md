# Source application inventory and Django rewrite plan

This branch is a fast-running prototype of the core work-order workflow. It does **not** claim complete feature or operational parity with the .NET application. This catalog records the source areas inspected and a native Python mapping to guide staged parity work.

## Source repository inventory

The checked-in [`django-source-inventory.csv`](django-source-inventory.csv) lists every repository file path discovered by `git ls-files`, with its type, source area, intended Python destination, and port status. Regenerate it after repository changes with `python python-django/scripts/generate_source_inventory.py`. At the original inspected `master` revision the repository contained 1,362 tracked files: 765 under `src/`, 100 feature proposals, 79 architecture artifacts, 30 operations and stability docs, 22 codebase audit artifacts, 19 GitHub workflows/scripts, 18 labs, 16 OpenSpec documents, and 91 video project files, plus configuration and tooling. The CSV is the file-by-file catalog; the area table below summarizes the mapping.

| Existing code area | What it contains | Django track mapping |
|---|---|---|
| `src/Core/` | Domain entities, status and state commands, validations, query specifications, number and urgency rules, CSV parsing, request/event contracts | `workorders/models.py`, `services.py`, forms and validators; domain tests in Django `TestCase` |
| `src/DataAccess/` | EF Core persistence, MediatR command/query handlers, SQL query integration | Django ORM/querysets and explicit service functions; add PostgreSQL integration tests |
| `src/Database/` | DbUp migrations, database rebuild console, SQL deployment scripts and ARM resources | Django schema migrations; current prototype creates an isolated SQLite file on each private build |
| `src/UI/Client/`, `src/UI/Shared/` | Blazor WebAssembly pages/components, work-order search/manage, settings, sessions, speech/attachments/notifications | Django templates/forms first; HTMX/Alpine or React only if the UI needs richer interaction |
| `src/UI/Server/` | Blazor host, auth, diagnostics, health, middleware, caching, rate limits, API host, gRPC and real-time notifications | Django middleware, Django REST Framework (API), Channels (WebSockets) where needed |
| `src/UI/Api/` | REST endpoints for work orders, bulk import, diagnostics, health, feature flags, metrics and developer tools | Django REST Framework serializers/viewsets; schema via drf-spectacular; API key/auth integration still open |
| `src/Worker/` | Background hosted service, message handling, automated work-order agent and bus integration | Celery + Redis/RabbitMQ; Django management commands for scheduled/manual tasks |
| `src/LlmGateway/` | Azure OpenAI chat/translation integration, tool calls and tracing | Official Azure OpenAI / OpenAI Python SDK, provider interface, OpenTelemetry instrumentation |
| `src/McpServer/` | MCP tools/resources for work orders and employees | MCP Python SDK; keep a separate ASGI service or mount the SDK transport if supported |
| `src/ChurchBulletin.AppHost/`, `src/ServiceDefaults/` | .NET Aspire local orchestration and shared service defaults/telemetry | Docker Compose for local dependencies; OpenTelemetry Python and standard env-based configuration |
| `src/AcceptanceTests/` | Browser acceptance tests and Playwright run settings | `playwright` Python sync API; visible mode via `HEADFUL=1` plus Xvfb on Linux CI |
| `src/UnitTests/`, `src/IntegrationTests/` | xUnit unit/component/web/database/MCP/integration tests; bUnit UI tests; fixtures and shared SQL setup | Django `TestCase` for domain and request tests; pytest/pytest-django for expanded suites; Playwright for browser acceptance |
| `src/Database/scripts/`, `scripts/`, root PowerShell files | SQL deployment, environment setup, build orchestration, package creation, CRAP checks | `python-django/PrivateBuild.ps1` and `python-django/scripts/private_build.sh`; Alembic is unnecessary because Django owns schema migrations |

## Application capabilities found in source

The active product model includes employees and roles; work orders with draft/assigned/in-progress/complete/cancelled states; creators, assignees, numbers, descriptions, instructions, rooms, due dates, attachments, status transition commands and event/audit history. Search and work-order-by-number queries, status counts, CSV bulk import, due-date urgency, health/diagnostics and API endpoints are also present. UI code includes work-order create/manage/search, user sessions and login, settings/theme, speech and dictation, attachments, chat/AI, notification and real-time bus clients.

Additional systems in the source include the background worker, Azure OpenAI chat and translation, MCP tools/resources, gRPC work-order access, service health/metrics, and developer utility endpoints. These are source features; this Django prototype currently implements only the primary CRUD/lifecycle slice and a health endpoint.

The 100 files in `feature-proposals/` are future proposals and should not be treated as shipped requirements. They cover reporting and summaries, status timeline, saved search, bulk status, notes/subtasks/attachments, templates, integrations/webhooks/email/Slack, employee/admin management, user preferences, imports/exports, layout/accessibility, due dates/urgency, and API/auth extensions. Assess each proposal against its own acceptance criteria before porting it.

## Build and environment contract

### Existing `PrivateBuild.ps1`

Root `PrivateBuild.ps1` dot-sources `build.ps1`, runs `Build`, then enforces `scripts/crap/run-crap-audit.ps1 -SkipTests -FailOnViolations` using the threshold in `scripts/crap/crap-gate-threshold.json`. Root `build.ps1` requires PowerShell 7 and .NET 10, cleans/restores/builds `src/ChurchBulletin.sln` with warnings as errors, runs xUnit unit tests, resolves SQL-Container/LocalDB/SQLite, provisions a clean database, applies DbUp migrations, and runs integration tests. It packages the UI, database, acceptance tests and scripts. `AcceptanceTests.ps1` invokes the browser acceptance suite; `-Headful` turns headless browser mode off.

Database choice depends on platform and Docker availability. Linux with a Docker daemon uses SQL Server container; ARM or explicit `DATABASE_ENGINE=SQLite` uses SQLite. `SQL_EXTERNAL=true` uses an existing SQL Server instead of starting a container. Windows can use LocalDB. The source README documents .NET 10 and Docker setup, SQLite fallback, app URL `https://localhost:7174`, and acceptance test installation. CI `build.yml` runs the integration build and related gates; the workflow includes Linux and Windows build/package/test/coverage jobs. `deploy.yml` gates Octopus/TDD deployment on the established package artifact, then runs acceptance testing against TDD. These existing .NET and production release gates remain enabled on this branch and are not yet a Python deploy pipeline.

### Python private build in this branch

`python-django/PrivateBuild.ps1` and `python-django/scripts/private_build.sh` create/use a Python virtual environment, install compatible Django/Playwright dependencies, remove and recreate a clean SQLite database, run Django configuration checks, apply migrations, and execute the Django test suite. Pass `-Acceptance` to the PowerShell entry point or set `RUN_ACCEPTANCE=1` for the shell script to install Chromium and run the full browser flow. `python-django/AcceptanceTests.ps1` is the matching standalone PowerShell acceptance wrapper and supports `-Headful`. The root `PrivateBuild.ps1` and `AcceptanceTests.ps1` remain .NET-specific and unchanged on this prototype branch; running them at repository root still exercises the original system. This separate prototype build contract does not replace or claim to satisfy the root .NET private build.

## Native tools by responsibility

| Current .NET / operations tool | Python/Django counterpart |
|---|---|
| .NET SDK and solution build | CPython 3.12, `pip`, virtualenv, `manage.py check` |
| EF Core + DbUp + SQL scripts | Django ORM + Django migrations; PostgreSQL through `psycopg` for deployment |
| xUnit | Django `TestCase`; optionally pytest + pytest-django for larger fixtures and plugins |
| bUnit | Django request/template tests; Playwright for client-visible UI behavior |
| Playwright .NET | Playwright Python, Chromium; videos and screenshots captured in acceptance script |
| MediatR CQRS | Django service layer with explicit command/query functions; `django-river` or a small transition service only if a formal state machine is needed |
| Lamar / .NET DI | Django app registry/settings and constructor/configuration patterns; avoid adopting a separate container without need |
| BackgroundService / in-process bus | Celery + Redis/RabbitMQ for durable asynchronous work; Django signals only for local side effects |
| Aspire and Docker-based dependencies | Docker Compose for local services; OpenTelemetry SDK/OTLP exporter |
| Azure OpenAI .NET client | Azure OpenAI Python SDK; keep calls behind a provider service and test with fakes |
| .NET MCP server | Official MCP Python SDK with tools/resources and protocol acceptance tests |
| gRPC/protobuf | `grpcio` + `grpcio-tools`, generated Python stubs; preserve proto compatibility where clients depend on it |
| PowerShell database and package scripts | Python CLI modules/Typer plus small PowerShell wrappers for Bootcamp entry points |
| Qodana / .NET analyzers / CRAP gate | Ruff, mypy, Bandit, coverage.py, radon/complexipy; introduce independent thresholds rather than claiming identical CRAP semantics |
| GitHub Actions .NET build/package/upload | Python setup, pip cache, Django checks/tests, Playwright browser install, upload-artifact; existing legacy workflow still runs on the repository |
| Octopus package/deployment, Container Apps ARM | Docker image + Azure Container Apps/managed PostgreSQL, with IaC in Bicep/Terraform; not implemented in this prototype |

## Implemented and missing parity

Implemented: employee and role persistence, passwordless employee-picker login with uppercase dropdown names, the `tlovejoy` shortcut, database-backed sessions across navigation, and logout; work-order persistence, generated `WO-000001` style numbers, creation form, work-order list, text/room/number search, status filter/count cards, detail page, actor- and begin-state-guarded transitions, timestamps and transition event history, JSON health response, schema migrations, domain/request tests, and a Playwright browser acceptance workflow. State actions match the source identity rules: draft assign/cancel require the creator, assigned cancel requires the creator, and begin/shelve/complete require the assignee. New-work-order navigation follows `CanCreateWorkOrder`; this Django prototype additionally rejects direct create requests for users without that role as a server-side hardening beyond the source menu's visibility check. `CanFulfillWorkOrder` is modeled but does not gate source state commands. Due-date urgency follows the Chicago calendar date at read time, showing due-today/overdue color and badges and supporting an overdue-only list filter; urgency is not persisted, completed/cancelled orders are not urgent, and a missing due date has no badge. Room numbers accept up to 900 characters, matching the source model. SQLite provides a no-service local run.

Not implemented yet: edit/reassignment rules, attachment metadata/display (source has metadata only, no binary upload), bulk CSV import/export, complex query/filter/sort/pagination parity, dashboards/reports, localization and speech, chat/translation, MCP, gRPC, API versioning/key auth/idempotency/rate limits, realtime notifications, worker/AI automation, detailed health/metrics, SQL Server/Azure infrastructure, packaging, production release/deploy, and the full 100-proposal acceptance backlog. Existing .NET workflows still exercise the original app and do not prove Python parity.

## Test evidence and recording delivery

Current local validation: `python manage.py check`, `python manage.py makemigrations --check --dry-run`, and `python manage.py test` pass (18 tests). Playwright Python is installed, but Chromium is unavailable in this sandbox; the CI workflow installs Chromium on the runner and runs browser acceptance under Xvfb, captures `.webm` plus screenshot, renders a Remotion MP4, and uploads all three as `django-work-order-acceptance`.
