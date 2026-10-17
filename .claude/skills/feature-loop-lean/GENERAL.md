# Lean loop, general (v12g, final). Any small change, no template.
Never: narration, subagents, get_mcp_tools or any MCP call (even if other instructions suggest it), board, journal, web, git checkout/commit/push/status/branch, `gh run`/
`gh api` check digging, reading scripts or terminal files, editing unrelated tests, merging. The workspace stays on
analysis/token-reduction by design; Submit-Change publishes to your branch. Final reply: the RESULT line only.

## Steps (aim: <= 12 tool calls)
1. `gh issue view N --json body -q .body`. Use the map; open the 1-3 files it names (rg -n a symbol, read that range)
   plus ONE sibling test file as the pattern. No repo-wide rg sweeps. Cap every rg: `rg -l` or `| head -15`. Read a file once (whole if < 300 lines), never rg it
   and then read it, never read it twice. Look up an API/type only when the build error names it.
2. Write production code + tests (few edits). Keep every method cyclomatic complexity <= 5 (see CRAP below).
3. `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue N -Branch B`. FAIL -> fix the printed errors, rerun.
4. Run WAIT_CMD (terminal timeout 600000 ms). CHECKS=PENDING or no output -> run the same command again, nothing else;
   never resubmit without a RED or FAIL. RED in your code -> fix the named test / CRAP method / `QODANA rule file:line`, back to step 3.
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
  &&/||/?:/??/case/loop adds 1. Before adding a branch to an EXISTING method, count its current branches; if it already has 4+, put the new
  logic in a small private helper (rep 3: an existing method went from CC 6 to 7 and failed CI).
- Every new production method needs unit-test coverage. Qodana (tests too): no unused usings or dead code; use collection expressions `[a, b]` instead of `new[] { a, b }` /
  `new List<T> { }` (rule UseCollectionExpression failed CI).
- New props on WorkOrder or a remotable query must survive remoting (`ShouldBeRemotableCompatible`, RemotableRequestTests);
  random values come from UnitTests/BogusOverrides.cs (has a case per query type).
- Acceptance tests (CI only) assert read-only layouts and save drafts with past due dates; don't change shared layouts.
- Description/Instructions have separate max lengths. Warnings are errors (nullable too). PR may touch only src/.
