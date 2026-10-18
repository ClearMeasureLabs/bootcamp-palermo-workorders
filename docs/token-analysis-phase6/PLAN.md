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

## Round 2 (v15g) items
| Item | Issue | Kind |
|---|---|---|
| s01 | #10270 | domain method (GetAgeInDays) |
| s02 | #10271 | validation rule (user name length) |
| s03 | #10272 | Blazor page (Counter decrement) |
| s04 | #10273 | handler ordering + integration test |
| s05 | #10274 | DB script + mapping + integration test |
| s06 | #10275 | LLM gateway config validation (branch in existing method) |
| s07 | #10276 | MCP tool input handling + integration tests |
| s08 | #10277 | REST option on existing controller (branch in existing method) |

Round 2 launch prompt (v15g; Submit no longer waits): replace the round-1 prompt's middle with
"Use `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue N -Branch cursor/token-exp-p6-sNN-k2`, then run the printed WAIT_CMD until CHECKS=GREEN or CHECKS=RED." (full text in the report / below)

> Follow `.claude/skills/feature-loop-lean/GENERAL.md` for issue #N on branch cursor/token-exp-p6-sNN-k2. Do only what it says. Use `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue N -Branch cursor/token-exp-p6-sNN-k2`, then run the printed WAIT_CMD until CHECKS=GREEN or CHECKS=RED. Never call get_mcp_tools. No narration, subagents, MCP, git, board or journal. Do NOT merge. Reply with only the RESULT line: `RESULT=GREEN|RED PR=<url>`.

## Round 3 (v16g) items
| Item | Issue | Kind |
|---|---|---|
| t01 | #10286 | domain (WorkOrderStatus.IsTerminal) |
| t02 | #10287 | domain (Employee.IsInRole) |
| t03 | #10288 | Blazor page empty state (FetchData) |
| t04 | #10289 | REST option on existing endpoint (time ?format=unix) |
| t05 | #10290 | handler change + integration test (case-insensitive lookup) |
| t06 | #10291 | DB script + mapping + integration test |
| t07 | #10292 | MCP tool parameter + integration test |
| t08 | #10293 | UI bug from symptom (tooltip wording) |

> Follow `.claude/skills/feature-loop-lean/GENERAL.md` for issue #N on branch cursor/token-exp-p6-tNN-k3. Do only what it says. Use `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue N -Branch cursor/token-exp-p6-tNN-k3`, then run the printed WAIT_CMD until CHECKS=GREEN or CHECKS=RED. If a script fails for a reason outside your code, reply SCRIPT_FAIL with its last line and stop. Never call get_mcp_tools. No narration, subagents, MCP, git, board or journal. Do NOT merge. Reply with only the RESULT line: `RESULT=GREEN|RED PR=<url>`.

## Round 4 (v17g, 9e1be46f)
| Item | Issue | Branch |
|---|---|---|
| u01 HasInstructions | #10302 | cursor/token-exp-p6-u01-k4 |
| u02 HasEmailAddress | #10303 | cursor/token-exp-p6-u02-k4 |
| u03 Counter Reset | #10304 | cursor/token-exp-p6-u03-k4 |
| u04 longest word | #10305 | cursor/token-exp-p6-u04-k4 |
| u05 count by status | #10306 | cursor/token-exp-p6-u05-k4 |
| u06 Role name 100 | #10307 | cursor/token-exp-p6-u06-k4 |
| u07 list-work-orders status | #10308 | cursor/token-exp-p6-u07-k4 |
| u08 Health Check tooltip | #10309 | cursor/token-exp-p6-u08-k4 |

Launch prompt: Follow `.claude/skills/feature-loop-lean/GENERAL.md` for issue #N on branch cursor/token-exp-p6-uNN-k4. Do only what it says. Start with `pwsh -NoProfile scripts/agent/Get-Context.ps1 -Issue N`. Use `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue N -Branch cursor/token-exp-p6-uNN-k4`, then run the printed WAIT_CMD until CHECKS=GREEN or CHECKS=RED. If a script fails for a reason outside your code, reply SCRIPT_FAIL with its last line and stop. Never call get_mcp_tools. No narration, subagents, MCP, git, board or journal. Do NOT merge. Reply with only the RESULT line: `RESULT=GREEN|RED PR=<url>`.

## Round 5 (v18g, e5fb4d33)
All targets were checked against master and do not exist yet.
| Item | Issue | Branch |
|---|---|---|
| w01 WorkOrder.HasRoomNumber | #10317 | cursor/token-exp-p6-w01-k5 |
| w02 Employee.GetLastFirstName | #10318 | cursor/token-exp-p6-w02-k5 |
| w03 forecast count caption | #10319 | cursor/token-exp-p6-w03-k5 |
| w04 list-work-orders titleContains (MCP, retests u07 path) | #10320 | cursor/token-exp-p6-w04-k5 |
| w05 EmployeeCountQuery | #10321 | cursor/token-exp-p6-w05-k5 |
| w06 hash uppercase flag | #10322 | cursor/token-exp-p6-w06-k5 |
| w07 Ping HEAD | #10323 | cursor/token-exp-p6-w07-k5 |
| w08 Role.HasFullAccess | #10324 | cursor/token-exp-p6-w08-k5 |

Launch prompt: Follow `.claude/skills/feature-loop-lean/GENERAL.md` for issue #N on branch cursor/token-exp-p6-wNN-k5. Do only what it says. Start with `pwsh -NoProfile scripts/agent/Get-Context.ps1 -Issue N`. Submit with `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue N -Branch cursor/token-exp-p6-wNN-k5`; it waits for CI itself. Only if it printed no CHECKS=GREEN/RED, run the printed WAIT_CMD until it does. If a script fails for a reason outside your code, reply SCRIPT_FAIL with its last line and stop. Never call get_mcp_tools. No narration, subagents, MCP, git, board or journal. Do NOT merge. Reply with only the RESULT line: `RESULT=GREEN|RED|ALREADY_DONE PR=<url or evidence>`.
