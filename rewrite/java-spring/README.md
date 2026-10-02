# Java + Spring Boot rewrite prototype

This isolated prototype uses Java 17, Spring Boot 3, Spring Data JPA, Bean Validation, Thymeleaf, H2 for local/private builds, and PostgreSQL-compatible configuration for deployment. The work order domain rules live in `domain/`, use cases in `application/`, storage in `persistence/`, and HTTP/UI adapters in `web/`.

## Run and verify

Requirements: JDK 17+ and Maven 3.9+.

```sh
cd rewrite/java-spring
mvn clean verify
SPRING_PROFILES_ACTIVE=dev mvn spring-boot:run
```

Open <http://localhost:8080>. The username-only employee picker is enabled only for the `dev` and `test` profiles. It is a development/demo convenience, not production authentication; a deployed application needs an external identity provider before enabling protected user flows. Persistent local H2 data is stored under `rewrite/java-spring/data/`. To connect to PostgreSQL, set `DATABASE_URL=jdbc:postgresql://host:5432/workorders` and add the PostgreSQL driver before deployment; local H2 is the current prototype default.

`PrivateBuild.ps1` provisions the local H2 schema through JPA, compiles, and runs unit/integration checks. `AcceptanceTests.ps1` runs the prototype's UI acceptance suite; it records Chromium video under `target/playwright-recordings/`. CI uses Xvfb for headful browser mode and uploads that recording as an artifact. Remotion compiles the recorded WebM to H.264 MP4 in CI and uploads both capture and MP4 as an artifact. These scripts are new Java equivalents; they do not replace the repository's original root `.NET` scripts. The root `PrivateBuild.ps1` still builds the original application.

## Implemented slice

- Create work orders as drafts, list and filter by state, and read an order by number.
- Lifecycle transitions: draft → assigned → in progress → complete; open work orders may be cancelled.
- Validation for required title/assignee and field length limits; draft creation persists description, instructions (4000 characters), room (900 characters), creator, and optional date-only due date.
- Read-time due-date urgency in the `America/Chicago` calendar zone, with due-today/overdue styling and badges for open orders; completed/cancelled orders have no urgency and no-date orders have no badge.
- Optional overdue-only filter in the HTML list and JSON API; filtering is executed in the database and excludes closed orders.
- Browser UI for create, status filtering, assignment, start, completion, and cancellation.
- JSON API at `/api/work-orders` with create/list/get and lifecycle actions.
- Employee selection sign-in with uppercase names, a Timothy Lovejoy shortcut, logout, and server-side session identity that survives page reloads. This intentionally mirrors the source demo picker and is available only in the `dev` and `test` profiles; it does not use passwords or represent a production identity provider. Cookie-session mutations require a session-bound CSRF token, and work-order reads require a selected employee session.
- Employees and roles persisted through Flyway. Create and assignee choices honor the source `CanCreateWorkOrder` / `CanFulfillWorkOrder` flags; the Java API also rejects unauthorized role use. Lifecycle endpoints validate the session employee against the stored creator or assignee, matching the source state-command actor rules.
- Attachment metadata can be recorded and listed with file name, content type, byte size, uploader, and UTC timestamp. Binary file upload/storage is not implemented.
- H2-backed persistence with versioned Flyway migrations and Hibernate schema validation, plus unit, Spring MVC integration, and Chromium/Playwright browser acceptance with a recorded browser video.

## Inventory of source behavior reviewed

The source tree is substantially larger than a CRUD work-order app. Its top-level product areas include:

- `src/Core`: work-order/employee domain model, state commands/events, validators, search/import/number-generation services, dated order generation, due-date rules, attachments.
- `src/DataAccess`: EF Core mappings/context, MediatR handlers, database upgrade tooling, telemetry handlers, SQL/SQLite support.
- `src/UI/Server`, `src/UI/Api`, `src/UI.Shared`, `src/UI.Client`: Blazor UI, API controllers/middleware, authentication, health/metrics, rate limits, realtime, gRPC, bulk import, chat/speech/AI workflows.
- `src/Worker`: NServiceBus endpoint, message handlers, AI bot saga and persistence.
- `src/McpServer`, `src/LlmGateway`: MCP endpoints/tools and Azure OpenAI/chat tools.
- `src/UnitTests`, `src/IntegrationTests`, `src/AcceptanceTests`: NUnit unit/component/API tests, database/host integration tests, and Playwright browser acceptance tests (including AI/MCP and NServiceBus flows).
- `src/Database/scripts/Update`: ordered SQL Server schema/data migrations; `src/Database/Console`: upgrade/rebuild/baseline runner.
- `arch/`, `codebase-audit-report/`, `labs/`: PlantUML/Mermaid architecture and workflows, audit metrics, teaching labs and walkthroughs.
- `.github/workflows/`, `azure-pipelines.yml`/pipeline files, `PrivateBuild.ps1`, `build.ps1`, `BuildFunctions.ps1`, `AcceptanceTests.ps1`: CI/deployment, private environment setup, build database selection, acceptance launch and cleanup.

Runtime behavior and feature cases found in source/tests include login/logout, work-order draft/save/search/filter, assignment, begin, completion, cancellation, shelving, due dates/overdue/dashboard counts, instructions/room number limits, attachments, CSV bulk import, dictation/speech, AI chat/reformat agents, employee/role settings, status counts, health/readiness/metrics, API versioning/rate limits/idempotency/ETags, realtime notifications, gRPC and MCP operations, background AI saga, dated batches, database migration/rebuild, deployment verification and observability. `SOURCE_INVENTORY.csv` lists every first-party C#, Razor, SQL, protobuf, and server-rendered/web source file with its intended Java destination and current port status. Regenerate it from the repository root with `python3 rewrite/java-spring/scripts/generate_source_inventory.py`. The current Java prototype covers a narrow create/list/filter/read/lifecycle slice and due-date urgency; parity is not claimed for the remaining capabilities.

## Private build, ops and test mapping

| Current capability/tool | Java-native direction | Prototype status |
|---|---|---|
| `PrivateBuild.ps1` + `build.ps1` create DB environment, restore, compile, unit/integration, CRAP gate | Maven `clean verify`, Spring Boot/JPA schema initialization, Testcontainers PostgreSQL for CI | Local H2 setup and Maven verify script added; Testcontainers and static complexity gate still open |
| SQL Server LocalDB, Docker SQL Server, SQLite fallback | PostgreSQL for production; H2 for fast local; Testcontainers PostgreSQL for realistic CI; SQL Server JDBC only if SQL Server remains a deployment constraint | H2 local and PostgreSQL driver configured; no SQL Server schema migration parity or PostgreSQL Testcontainers gate |
| EF Core and ordered SQL scripts / Database console | Spring Data JPA + Flyway versioned migrations | Flyway V1 schema, V2 instructions-column guard, V3 employee/role and ownership schema with display-name backfill, V4 attachment metadata schema, V5 optimistic-lock version, and Hibernate `validate` implemented for H2/PostgreSQL; original migration history and upgrade/rebuild tooling are not ported |
| Username-only employee picker, custom Blazor auth state, and logout | Thymeleaf login form backed by a server-side `HttpSession`, employee lookup, and session invalidation | Demo employee chooser and Lovejoy shortcut; session username is reloaded from the database on each protected operation. No password or external identity provider is implemented |
| `StateCommandBase.UserCanExecute` creator/assignee actor checks and role capability flags | Application service authorization based on session employee, persisted work-order owner usernames, and employee role flags | Create is role-gated; assignment requires the creator and a fulfiller; cancel requires the creator; begin/shelve/complete require the assignee. Server-side create/fulfiller gates harden checks that the source UI primarily expresses through affordances |
| MediatR CQRS and Lamar DI | Spring application services, Spring DI, optionally Spring Modulith for module/event boundaries | Service and constructor injection implemented; async events not yet ported |
| NUnit + Shouldly + bUnit | JUnit 5 + AssertJ + Spring MVC Test; Thymeleaf UI does not need Blazor component harness | API tests cover sessions, authorization, lifecycle, due-date filtering, and metadata-only attachments |
| Playwright NUnit browser tests, `AcceptanceTests.ps1 -Headful` | Playwright for Java + JUnit 5; browser installation via Playwright CLI; headful mode via `headless=false` | Browser acceptance covers demo login/logout, session persistence, CSRF rejection, work-order lifecycle and instructions/urgency, and attachment metadata display; the browser recording and rendered MP4 are uploaded by CI |
| NServiceBus sagas + SQL transport | Spring Modulith events for in-process workflows; Spring Cloud Stream/Kafka or RabbitMQ + outbox for durable distributed workflows; Temporal/Camunda for long-running orchestration | No worker/saga port yet |
| MCP C# SDK and Azure OpenAI | Official MCP Java SDK / Spring AI MCP and Spring AI Azure OpenAI integration | Not implemented |
| OpenTelemetry/Azure Monitor | Micrometer Observation + OpenTelemetry Java agent/exporter; Azure Monitor OpenTelemetry distro | Not implemented |
| Azure Container Apps, ARM templates, Azure DevOps/Octopus workflows | Container image + ACA Bicep/ARM; GitHub Actions; Maven artifact/image provenance; secret refs via ACA | Existing deployment workflows remain .NET; no Java deployment workflow |
| Remotion TypeScript video workflow | Keep Remotion/Playwright capture as separate Node tool; Java app provides deterministic test route/data | Java browser acceptance video is recorded and rendered to MP4 in CI; artifact links are recorded in the PR |

## CI and known limits

`.github/workflows/java-spring.yml` adds a Maven test/build gate. The previously published attachment milestone passed Java Maven tests, headful Chromium acceptance, and Remotion rendering in [run #35947173194](https://github.com/ClearMeasureLabs/bootcamp-palermo-workorders/actions/runs/35947173194) on commit `70ae4be774ef497102b9967a2a3424ac0783f4a0`; the pending CSRF and persistence-hardening changes require a new run before their status is known. This environment does not include Maven, PowerShell, Docker, or a browser installation, so verification was performed by GitHub Actions. Root .NET checks are separate and do not establish Java parity; the Java branch remains a partial rewrite. The Java workflow uploads the browser recording and rendered MP4 as an artifact.
