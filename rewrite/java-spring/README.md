# Java + Spring Boot rewrite prototype

This isolated prototype uses Java 17, Spring Boot 3, Spring Data JPA, Bean Validation, Thymeleaf, H2 for local/private builds, and PostgreSQL-compatible configuration for deployment. The work order domain rules live in `domain/`, use cases in `application/`, storage in `persistence/`, and HTTP/UI adapters in `web/`.

## Run and verify

Requirements: JDK 17+ and Maven 3.9+.

```sh
cd rewrite/java-spring
mvn clean verify
mvn spring-boot:run
```

Open <http://localhost:8080>. Persistent local H2 data is stored under `rewrite/java-spring/data/`. To connect to PostgreSQL, set `DATABASE_URL=jdbc:postgresql://host:5432/workorders` and add the PostgreSQL driver before deployment; local H2 is the current prototype default.

`PrivateBuild.ps1` provisions the local H2 schema through JPA, compiles, and runs unit/integration checks. `AcceptanceTests.ps1` runs the prototype's UI acceptance suite; it records Chromium video under `target/playwright-recordings/`. CI uses Xvfb for headful browser mode and uploads that recording as an artifact. Remotion compiles the recorded WebM to H.264 MP4 in CI and uploads both capture and MP4 as an artifact. These scripts are new Java equivalents; they do not replace the repository's original root `.NET` scripts. The root `PrivateBuild.ps1` still builds the original application.

## Implemented slice

- Create work orders as drafts, list and filter by state, and read an order by number.
- Lifecycle transitions: draft → assigned → in progress → complete; open work orders may be cancelled.
- Validation for required title/assignee and field length limits.
- Browser UI for create, status filtering, assignment, start, completion, and cancellation.
- JSON API at `/api/work-orders` with create/list/get and lifecycle actions.
- H2-backed persistence and unit, Spring MVC integration, and Chromium/Playwright browser acceptance with a recorded browser video.

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

Runtime behavior and feature cases found in source/tests include login/logout, work-order draft/save/search/filter, assignment, begin, completion, cancellation, shelving, due dates/overdue/dashboard counts, instructions/room number limits, attachments, CSV bulk import, dictation/speech, AI chat/reformat agents, employee/role settings, status counts, health/readiness/metrics, API versioning/rate limits/idempotency/ETags, realtime notifications, gRPC and MCP operations, background AI saga, dated batches, database migration/rebuild, deployment verification and observability. The current Java prototype only covers the core create/list/filter/read/lifecycle slice; parity is not claimed for these other capabilities.

## Private build, ops and test mapping

| Current capability/tool | Java-native direction | Prototype status |
|---|---|---|
| `PrivateBuild.ps1` + `build.ps1` create DB environment, restore, compile, unit/integration, CRAP gate | Maven `clean verify`, Spring Boot/JPA schema initialization, Testcontainers PostgreSQL for CI | Local H2 setup and Maven verify script added; Testcontainers and static complexity gate still open |
| SQL Server LocalDB, Docker SQL Server, SQLite fallback | PostgreSQL for production; H2 for fast local; Testcontainers PostgreSQL for realistic CI; SQL Server JDBC only if SQL Server remains a deployment constraint | H2 local only; no SQL Server schema migration parity |
| EF Core and ordered SQL scripts / Database console | Spring Data JPA + Flyway versioned migrations | JPA/H2 implemented; Flyway and ported migrations not yet implemented |
| MediatR CQRS and Lamar DI | Spring application services, Spring DI, optionally Spring Modulith for module/event boundaries | Service and constructor injection implemented; async events not yet ported |
| NUnit + Shouldly + bUnit | JUnit 5 + AssertJ + Spring MVC Test; Thymeleaf UI does not need Blazor component harness | JUnit/Spring MockMvc tests added |
| Playwright NUnit browser tests, `AcceptanceTests.ps1 -Headful` | Playwright for Java + JUnit 5; browser installation via Playwright CLI; headful mode via `headless=false` | The acceptance test creates a work order through the browser form, then exercises assignment in Chromium and records video; Remotion MP4 rendering is wired in CI; execution still needs verification |
| NServiceBus sagas + SQL transport | Spring Modulith events for in-process workflows; Spring Cloud Stream/Kafka or RabbitMQ + outbox for durable distributed workflows; Temporal/Camunda for long-running orchestration | No worker/saga port yet |
| MCP C# SDK and Azure OpenAI | Official MCP Java SDK / Spring AI MCP and Spring AI Azure OpenAI integration | Not implemented |
| OpenTelemetry/Azure Monitor | Micrometer Observation + OpenTelemetry Java agent/exporter; Azure Monitor OpenTelemetry distro | Not implemented |
| Azure Container Apps, ARM templates, Azure DevOps/Octopus workflows | Container image + ACA Bicep/ARM; GitHub Actions; Maven artifact/image provenance; secret refs via ACA | Existing deployment workflows remain .NET; no Java deployment workflow |
| Remotion TypeScript video workflow | Keep Remotion/Playwright capture as separate Node tool; Java app provides deterministic test route/data | Existing repo contains Remotion projects; no Java prototype video produced |

## CI and known limits

`.github/workflows/java-spring.yml` adds a Maven test/build gate. The existing root `Build` workflow still runs the original .NET solution and its established jobs; that green result is not evidence of Java parity. The Java PR's branch protection/required checks and repository-wide Deploy-to-TDD status context need review before deployment. This environment does not include Maven, PowerShell, Docker, or a browser installation, so the Java build and real browser acceptance run could not be executed locally during authoring. The GitHub workflow installs Chromium under Xvfb, records Playwright, renders the recording with Remotion, and uploads both the browser capture and MP4. GitHub Actions is the intended executable gate, and this PR must not be described as green unless its Actions checks report success.
