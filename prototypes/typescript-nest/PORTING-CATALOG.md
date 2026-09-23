# TypeScript + NestJS rewrite spike

This directory is an executable vertical slice for evaluating a TypeScript rewrite. It demonstrates the work-order core and records how the current repository is built, run, tested, and operated. It is not a claim of feature parity with the full repository.

## Current application inventory

The upstream solution is `src/ChurchBulletin.sln` (the user-facing product is the Palermo work-order application despite the legacy `ChurchBulletin` assembly name). `README.md`, `arch/README.md`, `arch/DIAGRAMS.md`, and the PlantUML files under `arch/` are the architecture entry points. Source projects are:

| Current project / paths | Responsibilities and notable files |
|---|---|
| `src/Core/` | Domain entities, value types, state commands, events, query contracts, bus abstractions. `Model/WorkOrder.cs`, `WorkOrderStatus.cs`, `Employee.cs`, `Role.cs`, `WorkOrderAttachment.cs`, `Model/StateCommands/*`, `Queries/*`. |
| `src/DataAccess/` | EF Core mappings and context, MediatR handlers, query/search filters, persistence, messaging. `Mappings/DataContext.cs`, `Mappings/WorkOrderMap.cs`, `Handlers/StateCommandHandler.cs`, `WorkOrderSearchHandler.cs`. |
| `src/Database/` | DbUp schema provisioning, SQL scripts, SQL and Azure deployment definitions. `scripts/Install/*`, `scripts/Update/*`, `DatabaseARM.json`. |
| `src/UI/Server/` | Web host, authentication, REST diagnostics and bulk import API, gRPC, websocket notifications, idempotency, rate limits, API keys, logging and telemetry. `Program.cs`, `Api/*`, `Grpc/*`, `Notifications/*`, `Middleware/*`. |
| `src/UI/Client/`, `src/UI.Shared/` | Blazor WebAssembly and shared screens. Work-order manage/search/dashboard, login, AI chat, navigation, health checks, theme and session handling. |
| `src/Worker/` | Background host and bus endpoint for distributed work/order events. |
| `src/LlmGateway/` | Azure OpenAI client configuration/health, translation guards, chat, work-order tools, tracing. |
| `src/McpServer/` | MCP server, employee/work-order tools, reference resources, endpoint selection and loopback HTTP bridge. |
| `src/ChurchBulletin.AppHost/`, `src/ChurchBulletin.ServiceDefaults/` | .NET Aspire local orchestration and shared telemetry/health/service defaults. |
| `src/UnitTests/` | NUnit + Shouldly unit and host/web tests covering domain, API, database, UI server, worker, MCP, LLM, telemetry and build gates. |
| `src/IntegrationTests/` | NUnit integration suites for data access, HTTP APIs, database mappings, worker, MCP, LLM, gRPC and host wiring; supports SQLite and SQL Server cases. |
| `src/AcceptanceTests/` | NUnit + Microsoft Playwright browser system tests: create/save, assign, begin, complete/cancel, due dates, attachments, search, status dashboard, speech/dictation, AI chat, authentication, MCP, APIs and navigation/theme/health. |

The product flows found in the domain, UI and acceptance tests include: create/save draft; draft → assigned; assigned → in progress; in progress → complete; cancel and reassign; work-order number and search/filter; employee/role assignment; room, due date, instructions and attachment metadata; status dashboard; authentication/session; API health, diagnostics, feature flags, bulk import, metrics and gRPC; browser navigation/theme; speech/dictation; LLM chat and MCP tools; worker events and distributed messaging. The repository has 100 `feature-proposals/` files; these are proposed backlog items, not all implemented behavior.

## Existing run and build contract

- `README.md`: .NET 10 SDK, PowerShell 7+, SQL Server LocalDB on Windows or Docker SQL Server on Linux/macOS; SQLite is the no-Docker fallback. Aspire `src/ChurchBulletin.AppHost` is the local orchestration entry point. The server can also run directly from `src/UI/Server`.
- `build.sh`, `build.bat`, and `build.ps1`: thin platform entry points around `BuildFunctions.ps1`.
- `PrivateBuild.ps1`: dot-sources `build.ps1`, invokes the full `Build`, then reads `scripts/crap/crap-gate-threshold.json` and runs `scripts/crap/run-crap-audit.ps1 -SkipTests -FailOnViolations`. That private CRAP threshold/audit is an additional gate.
- `BuildFunctions.ps1` `Build`: requires `pwsh`; chooses SQL-Container, LocalDB, or SQLite; initializes the SQL Server PowerShell module; cleans and restores `src/ChurchBulletin.sln`; compiles with warnings as errors; runs unit tests; provisions/resets the build database and runs schema scripts; runs integration tests (excluding SQL-only tests for SQLite); then packages UI, database, acceptance-test and script artifacts. SQL-Container mode normally requires Docker; `SQL_EXTERNAL=true` uses an external server instead.
- `AcceptanceTests.ps1`: dot-sources `build.ps1` and calls `Invoke-AcceptanceTests`; `-Headful` sets `HeadlessTestBrowser=false`. The test project uses NUnit + Playwright, has browser installation/runtime prerequisites, launches the app/test server and browser, and includes auth, UI, API, work-order lifecycle, worker/MCP/AI scenarios. `build.ps1` has the authoritative implementation and options.
- GitHub Actions `.github/workflows/build.yml`: docs-change classifier, Linux SQL-container integration build, SQLite build, ARM SQLite build, Windows build, Qodana, Docker image/package/publish jobs and two acceptance suites. `.github/workflows/deploy.yml` has release/deployment checks; `render-diagrams.yml` checks architecture assets. These workflows remain .NET-focused on the prototype branch. The additional prototype workflow below validates this NestJS subtree only.
- Operations/developer inventory: `PrivateBuild.ps1`, `BuildFunctions.ps1`, `AcceptanceTests.ps1`, `build.{ps1,sh,bat}`, `scripts/crap/*`, `scripts/setup-dev-env.ps1`, `scripts/setup-dev-env.sh`, `Dockerfile`, `.github/workflows/*`, `arch/render-diagrams.{ps1,sh}`, `src/pure-azdo-pipeline.yml`, and `src/Database/scripts/*` (schema install/update plus Azure and pipeline SQL wrappers). `.bob/skills/ai-factory-executor/` supplies container/dev environment, executor/agent entrypoints, orchestration, test discovery, server start, issue run and PR monitoring. `.claude/skills/` and `.cursor/skills/` carry repo development, security, architecture and testing workflows.

## NestJS equivalents

| .NET responsibility | TypeScript equivalent recommended for a full rewrite |
|---|---|
| ASP.NET Core / DI / controllers / validation | NestJS modules/controllers/providers, `class-validator`, `class-transformer` |
| EF Core, migrations, DbUp | Prisma ORM + versioned Prisma migrations; PostgreSQL in production, SQLite for local/CI fast path. This spike uses pure-WASM `sql.js` to keep the proof-of-flow database portable in constrained Node environments. |
| MediatR CQRS | `@nestjs/cqrs` command/query buses, or explicit application services for simpler flows |
| Blazor WASM/shared UI | React + TypeScript, React Router, TanStack Query, accessible component library; keep UI separate from API |
| NUnit unit/web tests | Jest + `@nestjs/testing` + Supertest |
| Playwright acceptance | `@playwright/test` browser suites; traces/video artifacts in CI |
| Worker / NServiceBus / bus | NestJS standalone worker plus BullMQ/Redis or a managed broker (RabbitMQ/Azure Service Bus client) and transactional outbox |
| Aspire / health checks | Docker Compose for local services, `/health` + Terminus, OpenTelemetry SDK/exporters |
| Serilog / App Insights | Pino (`nestjs-pino`) + OpenTelemetry logs/traces/metrics |
| gRPC / proto | `@grpc/grpc-js` + `@grpc/proto-loader` or `ts-proto` |
| MCP SDK/server | `@modelcontextprotocol/sdk` (separate process/service) |
| PowerShell private build | npm scripts + Node orchestrator cross-platform; GitHub Actions matrix for Linux/Windows/ARM and containerized database services |
| CRAP gate / Qodana / dependency scans | `eslint`, `tsc --noEmit`, Jest coverage thresholds, SonarQube or CodeQL, `npm audit`/OSV scanner, Semgrep and secret scanning |

## Spike contents and commands

`src/` contains a NestJS health endpoint and work-order API/service; `sql.js` creates and persists a local SQLite database on service startup and after writes; `public/index.html` is a minimal real browser UI. API behavior includes draft creation, list/detail, filtering by status, validated state changes, required assignee at assignment, and assigned/completed timestamps. `src/work-orders/work-orders.e2e-spec.ts` exercises the HTTP API against SQLite. `scripts/acceptance.mjs` starts the service, records a Playwright browser video, and runs a real browser flow from create through complete. `scripts/private-build.mjs` is this prototype's private-build analogue: clean local SQLite environment, locked dependency install, TypeScript compile and automated test.

```sh
cd prototypes/typescript-nest
npm ci
npm run private-build
npm start                 # http://localhost:3000
npm run acceptance        # sets up DB, starts service and drives browser
HEADFUL=true npm run acceptance # visible browser where a display is available
```

The Playwright browser recording is written to `artifacts/` and remuxed into `artifacts/typescript-nest-acceptance.mp4` with Remotion by the demo-video workflow/tooling when Chromium, a display (for headful mode), and the Remotion renderer are installed.

## Known parity gaps

This spike implements only the core lifecycle screen and REST service. It does not port employee/role management, real authentication/authorization, search pagination/sorting, due-date rules, attachments, audit trail, bulk import, API key/rate-limit/idempotency contracts, gRPC, MCP, LLM/chat/speech, realtime notifications, distributed worker/outbox, telemetry/exporters, deployment/IaC, or the full original test inventory. SQLite and a demo assignee keep the slice runnable without provisioned services. The existing .NET checks do not validate this prototype; use `typescript-nest-prototype.yml` to gate it. A complete rewrite requires porting and proving each inventory item and replacing the repository root build/deployment contract after acceptance of this prototype.
