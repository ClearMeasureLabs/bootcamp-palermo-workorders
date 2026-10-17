# Lean loop, general (v10g). Any small change, no template.
Never: narration, subagents, get_mcp_tools/MCP, board, journal, web, git checkout/commit/push/status/branch, `gh run`/
`gh api` check digging, reading scripts or terminal files, editing unrelated tests, merging. The workspace stays on
analysis/token-reduction by design; Submit-Change publishes to your branch. Final reply: the RESULT line only.

## Steps (aim: <= 12 tool calls)
1. `gh issue view N --json body -q .body`. Use the map; open the 1-3 files it names (rg -n a symbol, read that range)
   plus ONE sibling test file as the pattern. No repo-wide rg sweeps; no re-reading a file you already read.
2. Write production code + tests (few edits). Keep every method cyclomatic complexity <= 5 (see CRAP below).
3. `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue N -Branch B`. FAIL -> fix the printed errors, rerun.
4. Run WAIT_CMD (terminal timeout 600000 ms). Empty output or CHECKS=PENDING -> run the same command again; nothing
   else. RED in your code -> fix the named test / CRAP method / `QODANA rule file:line`, back to step 3.
   RED only in unrelated tests -> `WAIT_CMD -ForceRerun` once. `PUBLISH_FAIL` -> reply with that line.

## Map (namespace ClearMeasure.Bootcamp.*; NUnit + Shouldly; test data via src/UnitTests/ObjectMother.cs)
| Change | Production | Tests (mirror the folder) |
|---|---|---|
| Domain entity/rule | src/Core/Model/ | src/UnitTests/Core/Model/ |
| State transition | Core/Model/StateCommands/ + Core/Services/Impl/StateCommandList.cs | UnitTests/Core/Model/StateCommands/ |
| Validation | Core/Validation/*Validator.cs (FluentValidation) | UnitTests/Core/Validation/ |
| Search filter | Core/Services/WorkOrderSearchSpecification.cs -> Core/Queries/WorkOrderSpecificationQuery.cs -> DataAccess/Handlers/WorkOrderQueryFilters.cs | UnitTests/Core/Queries/ ; IntegrationTests/DataAccess/WorkOrderSpecificationHandlerTests.cs |
| Other handler / EF | DataAccess/Handlers/ ; DataAccess/Mappings/*Map.cs | IntegrationTests/DataAccess/(Handlers,Mappings)/ |
| DB schema | Database/scripts/Update/NNN_Name.sql (DbUp; next = highest+1; copy 030's BEGIN TRY/TRAN shape) | none needed for index-only |
| Blazor UI | UI.Shared/Pages/, Components/, Models/ | UnitTests/UI.Shared/(Pages,Components)/ (bUnit) |
| REST API | UI/Api/Controllers/ | UnitTests/UI.Api/ ; IntegrationTests/Api/ |
| Server wiring | UI/Server/ | UnitTests/UI.Server/ |
| MCP tool | McpServer/Tools/*Tools.cs (static methods, [McpServerTool], [Description] per param; shared parsing in DatedWorkOrderScheduling.cs) | IntegrationTests/McpServer/ |
| LLM/chat | LlmGateway/ | UnitTests/LlmGateway/ |
| Logging | handlers take ILogger<T>; ServiceDefaults/ | No FakeLogger/testing package exists: write a small private capturing ILogger<T> in the test. |
| Worker / demo data | Worker/ ; IntegrationTests/ZDataLoader.cs | UnitTests/Worker/ ; IntegrationTests/DemoData/ |

## CI traps
- CRAP gate: threshold 6, CRAP = CC^2*(1-cov)^3 + CC, so even a 100%-covered method fails above CC 6. Each if/else-if/
  &&/||/?:/??/case/loop adds 1. Put new branches in a small private helper instead of growing an existing method.
- Every new production method needs unit-test coverage. Qodana: no unused usings, dead code, or redundant `new[]` types.
- New props on WorkOrder or a remotable query must survive remoting (`ShouldBeRemotableCompatible`, RemotableRequestTests);
  random values come from UnitTests/BogusOverrides.cs (has a case per query type).
- Acceptance tests (CI only) assert read-only layouts and save drafts with past due dates; don't change shared layouts.
- Description/Instructions have separate max lengths. Warnings are errors (nullable too). PR may touch only src/.
