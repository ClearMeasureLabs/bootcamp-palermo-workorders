# Lean loop, general (v9g, phase 4). Any small change, no template.
Rules: no narration, no subagents, no MCP/tool listing, no board/journal, no web. Never run git checkout/commit/push,
`gh run`, or log digging. Never edit tests unrelated to the change. Never merge. Final reply: the RESULT line only.

## Steps
1. Read the issue once: `gh issue view N --json body -q .body`. Pick files from the map below; open only those
   (use `rg -n` for a symbol, then read just that range). Read one sibling test as the pattern. No repo-wide listing.
2. Edit production code + tests in as few writes as possible.
3. `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue N -Branch B` (verifies, publishes, prints WAIT_CMD).
   BUILD/TEST/IT_BUILD FAIL -> fix exactly the printed errors, rerun step 3. (Verify only: add `-VerifyOnly`.)
4. Run WAIT_CMD until `CHECKS=GREEN` or `CHECKS=RED` (empty output -> run again). RED in your area -> fix the named
   tests / CRAP / `QODANA rule file:line`, rerun step 3. RED only in unrelated tests -> `WAIT_CMD -ForceRerun` once.
   `PUBLISH_FAIL` -> reply with that line.

## Codebase map (namespace ClearMeasure.Bootcamp.*; .NET, NUnit + Shouldly, Bogus via src/UnitTests/ObjectMother.cs)
| Change | Production | Tests |
|---|---|---|
| Domain entity/rule | src/Core/Model/ (WorkOrder, Employee, Role, WorkOrderStatus) | src/UnitTests/Core/Model/ |
| State transition | src/Core/Model/StateCommands/ + Core/Services/Impl/StateCommandList.cs | UnitTests/Core/Model/StateCommands/ |
| Request validation | src/Core/Validation/*Validator.cs (FluentValidation) | UnitTests/Core/Validation/ |
| Query (request) | src/Core/Queries/ ; spec: Core/Services/WorkOrderSearchSpecification.cs | UnitTests/Core/Queries/ |
| Handler / EF query | src/DataAccess/Handlers/ ; mapping: DataAccess/Mappings/*Map.cs | src/IntegrationTests/DataAccess/ (Handlers/, Mappings/) |
| DB schema | src/Database/scripts/Update/NNN_Name.sql (DbUp, next number = highest+1, BEGIN TRY/TRAN pattern of 030) | IntegrationTests/DataAccess/Mappings/ |
| Blazor UI | src/UI.Shared/Pages/*.razor(.cs), Components/, Models/ | UnitTests/UI.Shared/ (bUnit) ; acceptance: src/AcceptanceTests/ (CI only) |
| REST API | src/UI/Api/Controllers/ (route also under ApiRoutes.VersionedApiPrefix) | UnitTests/UI.Api/ ; IntegrationTests/Api/ |
| Server wiring/middleware | src/UI/Server/ | UnitTests/UI.Server/ |
| MCP tool | src/McpServer/Tools/*Tools.cs ([McpServerTool], [Description] on params) | IntegrationTests/McpServer/ |
| LLM/chat | src/LlmGateway/ | UnitTests/LlmGateway/ |
| Logging/telemetry defaults | src/ChurchBulletin.ServiceDefaults/ ; handlers take ILogger<T> | UnitTests/Logging/ , ServiceDefaults/ |
| Background jobs | src/Worker/ | UnitTests/Worker/ |
| Demo data | src/Database/ (seed) / IntegrationTests/ZDataLoader.cs | IntegrationTests/DemoData/ |

## CI traps (seen in earlier runs)
- New WorkOrder property must survive remoting (`ShouldBeRemotableCompatible`); add it to BogusOverrides if random.
- Every new production method needs a unit test (CRAP gate). No unused usings / dead code (Qodana).
- Acceptance tests (CI only) assert read-only layouts and save drafts with past due dates; don't change shared
  layouts or reject existing-data cases unless the issue says so.
- Description and Instructions have separate max lengths (`DescriptionMaxLength`, `InstructionsMaxLength`).
- Build is warnings-as-errors: nullable warnings fail the build.
- PR must touch only src/ (publish enforces it).
