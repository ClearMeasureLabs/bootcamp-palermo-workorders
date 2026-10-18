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

## Results (n01-n08 vs round 14 q01-q08, same work items, same v28g skill)
Measured exactly as round 14 (`measure_transcript.py` logic, `attribute_transcript_tokens.py`; bytes/4). 8/8 green.

| Item | Names run | Input | Round 14 | Input | Delta | 1st Get-Context output (KB) n / q | Reads n / q | Flow difference |
|---|---|---|---|---|---|---|---|---|
| 01 GetDisplayName | bc-0454d37e | 18.6k | bc-577b469b | 18.7k | -0.1k | 5.0 / 5.3 | 0 / 0 | q had an empty wait rerun; n had one more edit |
| 02 Counter decrement | bc-213e0bcf | 20.3k | bc-a1c6130a | 35.0k | -14.7k | 6.1 / 6.1 | 0 / 0 | q made 3 calls after GREEN (get_mcp_tools x2 + MCP), 15.0k |
| 03 MCP maxResults | bc-17c8a775 | 24.8k | bc-363df618 | 39.7k | -14.9k | 8.5 / 8.5 | 0 / 1 | q made a 2nd Get-Context (-Find) + 1 read of McpEmployeeToolTests |
| 04 EmployeeCountQuery | bc-7799e9ae | 37.1k | bc-b300cb29 | 35.7k | +1.4k | 10.4 / 9.2 | 0 / 0 | identical flow; n's Get-Context ranked different look-alikes (EmployeeQueryHandler + its 148-line test tail vs CanConnectToDatabaseHealthCheck) |
| 05 sentenceCount | bc-91411c14 | 30.8k | bc-c547d2ee | 42.0k | -11.2k | 7.9 / 8.0 | 0 / 0 | both hit CRAP_RISK + 2 Submits; q had an empty wait rerun and one more edit |
| 06 username whitespace | bc-f7be1786 | 12.5k | bc-ddfefca2 | 12.5k | 0.0k | 2.9 / 2.9 | 0 / 0 | identical |
| 07 Department column | bc-0d83be28 | 42.3k | bc-d6c16060 | 41.8k | +0.5k | 7.2 / 7.3 | 1 / 1 | identical (both read EmployeeMappingTests mid-edit) |
| 08 CompareTo(null) | bc-ebcec67f | 13.7k | bc-3b7c092e | 14.3k | -0.6k | 4.6 / 4.9 | 0 / 0 | identical |
| **Total** | | **200.1k** | | **239.7k** | **-39.6k (-16.5%)** | 52.6 / 52.2 | 1 / 2 | |

Per run: **25.0k** vs 30.0k. Attribution: reads before first edit 78.7k (39.3%), skill 42.1k (21.0%), assistant 35.0k (17.5%), edits 27.9k (14.0%), prompt 12.9k (6.4%).

## Separating the comment effect from noise
- **Direct effect on what agents read: none measurable.** The first Get-Context output (recipes + FULL files + test tails), the only place source text enters these runs, totals 52.6 KB vs 52.2 KB. Per item it moved by -0.3 to +1.2 KB. The files agents actually see (Employee.cs 75->69 lines, ToolsWordCountController 57->47, validators -2) had few comments to begin with. The comment-heavy code (UI/Api, UI/Server) is mostly not on these items' paths.
- **The -39.6k comes from three behaviour differences, none caused by comments:**
  - q02's 15.0k of calls after GREEN (an environment "check the message queue" prompt).
  - q03's extra `-Find` + read (about 15k).
  - Empty-wait reruns in q01 and q05.
- **The 4 items with identical flows (04, 06, 07, 08):** +1.4k, 0.0k, +0.5k, -0.6k, i.e. +1.3k net. Within noise. The +1.4k (04) is a Get-Context ranking side effect, not a saving.
- **Honest limit:** with 8 runs and run-to-run swings of about 15k from single behaviour events, an effect smaller than about 1-2k per run (5%) can't be detected. The direct byte measure above says the true effect on these items is close to zero.

## Verdict and recommendation
- **Verdict:** removing "what" comments did not reduce agent tokens on these work items. The 16.5% drop is noise from behaviour (calls after GREEN, an extra search, empty waits), not the refactor.
- **Recommendation:** drop it as a token lever. Keep v28g on analysis/token-reduction (src == master) as the baseline. A true descriptive-rename pass is unlikely to change tokens either, because the agents' reads are now one Get-Context call over a few small files.
- **If you still want a fair test:** use items whose files are comment-heavy (UI/Api controllers, UI/Server: about 590 comment lines). Run each item twice per arm to average out behaviour noise. Worth doing for readability, not tokens.
