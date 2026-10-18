# Phase 6 names experiment: comments -> descriptive names (branch analysis/token-names)

## Plan
- **Baseline:** v28g frozen, round 14 (q01-q08): 239.7k total, 30.0k/run (see round-14.md).
- **Change one variable, the source:** on `analysis/token-names` (off `analysis/token-reduction` 74cfd330), remove "what" comments and XML-doc restatements in the code agents read. Keep only "why" comments and ReSharper/Qodana directives. Where a comment labelled a block, give the code a descriptive name instead.
- **Same work:** the round 14 work-item texts are re-filed as n01-n08 (#10477-#10484), same skill (v28g) and same lean prompt, launched with `starting_ref: analysis/token-names`.
- **Measure** with the same scripts (bytes/4) and compare per item with q01-q08.

## What changed
- **Scope** (the dirs Get-Context / recipes surfaced in rounds 12-14): src/Core/{Model,Queries,Validation}, src/DataAccess, src/McpServer/Tools, src/UI/Api/Controllers, src/UI.Shared/Pages, src/UnitTests/{Core,UI.Api,UI.Shared/Pages}, src/IntegrationTests/{McpServer,DataAccess}. That is 216 .cs/.razor files.
- **Comment lines in scope:** 442 before (288 of them `///` XML doc), 109 after (0 XML doc). The 109 left are ReSharper/Qodana suppressions and "why" comments (e.g. the SQL Server nchar padding, the GroupBy-on-converter limit, the remoting validator requirement). 14 lines of "why" text from removed XML docs were kept as `//` comments.
- **Comment lines in all of src** (650 .cs/.razor, excl. Generated): 2122 before, 1789 after.
- **Removed:**
  - XML doc summaries (no project sets GenerateDocumentationFile, so they were inert).
  - `// Act/Assert/Arrange`, `// Configure ...`, `// Mock implementation - do nothing`.
  - Commented-out code.
  - Step-narration comments in tests.
- **Renamed / extracted:** ToolsWordCountController's "Normalize CRLF to LF ... count lines" comment became `CountLinesTreatingCrLfCrAndLfAsLineBreaks(text)`. Elsewhere the surrounding methods were already named for what the removed comment said (e.g. `RestoreAssignedToMeStateAsync`, `ApplyQueryParameterOverrides`). No public names (API JSON fields, MCP tool names, types) changed.
- **Files touched:** 52 src files (+18/-347). Also `scripts/agent/recipes.txt` (api-json-field range is now 8-47).
- **Scripts:**
  - Test-ChangeRisk diffs against `origin/analysis/token-names`, so the risk check sees only the agent's lines.
  - Publish-AgentPr still patches relative to `analysis/token-reduction` (whose src == master). Each PR therefore also carries the comment refactor, and CI checks it.
  - Why not publish only the agent's diff: a box test applying just an agent's diff to master conflicted next to the extracted method.

## Verification (box)
- `dotnet build src/ChurchBulletin.sln -c Release -warnaserror`: 0 errors on a clean worktree of the branch.
- Unit tests: 912 passed and 52 failed, and the same 52 fail on the pre-refactor commit. They are environment failures: the box has no SQL Server. The failing test names are identical before and after.
- Integration tests were not run on the box (they need a database). CI on the n01-n08 PRs will be the first full check of the refactor.
- Get-Context on #10481 (sentenceCount) and #10483 (Department) prints the recipe, NEW and NEXT_SCRIPT as before.

## Risk to comparability
- PR diffs are bigger (+ the refactor), but agents never read them; agent token cost is what is measured.
- If CI fails on the refactor itself (e.g. Qodana), the RED would hit all 8 runs. In that case, fix the branch and rerun rather than count those runs.

## Issues and launch
- n01 Employee.GetDisplayName #10477
- n02 Counter decrement #10478
- n03 MCP maxResults #10479
- n04 EmployeeCountQuery #10480
- n05 sentenceCount #10481
- n06 username whitespace #10482
- n07 Department column #10483
- n08 CompareTo(null) #10484

Branches `cursor/token-exp-p6-nNN-names`, starting_ref `analysis/token-names`.
