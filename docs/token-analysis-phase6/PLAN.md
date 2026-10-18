# Phase 6: structural levers (v14g), fresh items every round

Goal (Jeffrey, Oct 17): keep cutting tokens for ANY work in this codebase. Each round uses NEW items so gains generalize;
between rounds only the general skill/scripts change. Same measurement (`measure_transcript.py`, bytes/4).

## Where tokens went (phase 4 rep 3 + phase 5, 16 runs, 1,575k input; `scripts/agent/attribute_transcript_tokens.py`)
Each transcript line is charged its bytes times the number of later tool-call turns that replay it.
| Source | Input | Share |
|---|---|---|
| Reads/searches before the first edit | 728k | 46% |
| Assistant messages + tool args (replayed) | 281k | 18% |
| Skill file (4.8 KB, replayed every turn) | 280k | 18% |
| Edits | 110k | 7% |
| Issue body | 76k | 5% |
| Prompt | 51k | 3% |
| get_mcp_tools | 29k | 2% |
Cross-cutting: wait-call turns 366k (23%); the turns of 3 CI retry cycles (g04, v02, v07) about 221k (14%).

## v14g levers (savings are ESTIMATES against the 1,575k above)
| Lever | What | Estimated saving |
|---|---|---|
| Get-Context.ps1 | one call: files matching a regex (prod + tests) as line-numbered outlines; then read only ranges. Outlines of the 58 files read before first edit were 13% of their full size (40.5 KB vs 310 KB). | if half the pre-edit read bytes go: ~360k (~23%) |
| Test-ChangeRisk.ps1 in Submit | local checks for the traps behind every past CI RED: est. CRAP (CC > 5 on changed methods), `new[] {}`, options without the ReSharper line, Number > 7 chars in tests | up to the 221k retry share (~14%) |
| Changed integration tests on SQLite in Submit | runs the changed IntegrationTests classes locally (~3 s after build) | part of the retry share |
| Wait inside Submit | Submit waits for CI with its remaining ~9-min budget; removes the first separate wait turn | ~135k (~9%) |
| GENERAL.md 4.8 KB -> 2.1 KB | terse rules, one-line map, traps; tailored g02/g08 map lines removed | ~155k (~10%) |
These overlap, so they don't add up linearly. Not addressable from the skill: get_mcp_tools (platform), model thinking.

Tested on the box: Get-Context outputs; Test-ChangeRisk flags a planted CRAP/new[]/Number case in 0.4 s; Submit -VerifyOnly
ran unit tests + changed integration class on SQLite (IT_TEST=OK). The full unit suite has 52 failures on this box
on unmodified code (environment), so the full Submit path was not run here; CI VMs passed it in every earlier round.

## Round 1 items (r01-r08)
| Item | Issue | Kind |
|---|---|---|
| r01 | #10254 | server middleware + integration test (nosniff header) |
| r02 | #10255 | domain method (WorkOrder.IsAssignedTo) |
| r03 | #10256 | import parser rule (title > 300 in CSV) |
| r04 | #10257 | new REST endpoint (slugify) |
| r05 | #10258 | Blazor component a11y (aria-label) |
| r06 | #10259 | query + handler filter, cross-layer (employees by role) |
| r07 | #10260 | LLM gateway guard rule (no-letter text) |
| r08 | #10261 | worker logging (reply sent) |

Launch: model auto-smart, model_params {"optimize_for":"cost"}, starting_ref analysis/token-reduction,
branch cursor/token-exp-p6-rNN-k1 (k = round), title "token-exp p6 rNN k1". PRs closed unmerged.

## Launch prompt (v14g; fill N, NN, K)
> Follow `.claude/skills/feature-loop-lean/GENERAL.md` for issue #N on branch cursor/token-exp-p6-rNN-kK. Do only what it says. Use `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue N -Branch cursor/token-exp-p6-rNN-kK` (it also waits for CI); if it prints WAIT_CMD, run that until CHECKS=GREEN or CHECKS=RED. Never call get_mcp_tools. No narration, subagents, MCP, git, board or journal. Do NOT merge. Reply with only the RESULT line: `RESULT=GREEN|RED PR=<url>`.
