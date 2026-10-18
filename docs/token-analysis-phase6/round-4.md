# Phase 6 round 4 (v17g, 9e1be46f): u01-u08

Measured with `measure_transcript.py` + `attribute_transcript_tokens.py`, bytes/4. 7 green, 1 no PR (u07).

| Item | Issue | PR | Input | Output | Calls | Get-Context | Waits |
|---|---|---|---|---|---|---|---|
| u01 | #10302 | 10314 | 174.9k | 1.05k | 23 | 3 | 2 |
| u02 | #10303 | 10311 | 33.0k | 0.55k | 11 | 1 | 3 |
| u03 | #10304 | 10310 | 63.5k | 0.85k | 15 | 3 | 4 |
| u04 | #10305 | 10313 | 42.5k | 0.68k | 12 | 1 | 3 |
| u05 | #10306 | 10316 | 622.4k | 2.73k | 43 | 12 | 2 |
| u06 | #10307 | 10315 | 114.9k | 1.05k | 19 | 6 | 2 |
| u07 | #10308 | none | 98.2k | 0.86k | 15 | 6 | 0 |
| u08 | #10309 | 10312 | 90.6k | 0.74k | 17 | 4 | 2 |
| **Total** | | | **1,240.0k** | **8.51k** | 155 | 36 | 18 |

Per run: **155k** (all 8). Without u05: 617.6k / 7 = **88.2k**.

## Comparison (input per run)
| Reference | Per run | Round 4 (155k) vs it |
|---|---|---|
| Phase 6 round 3 (v16g) | 72k | +115% |
| Phase 6 round 1 / 2 | 296k / 378k | -48% / -59% |
| Phase 4 rep 3 / Phase 5 | 97k / 99.5k | +60% / +56% |
| Original baseline | 392k | -60% |

Round 4 is a regression from round 3, and u05 accounts for half of it. u02 (33.0k) and u04 (42.5k) are the cheapest runs so far: in both, `-Issue` found the right type on the first call.

## Causes
- **u07 PUBLISH_FAIL is not a script bug.** `McpServer/Tools/WorkOrderTools.cs:16-26` already had an optional `status` filter with a Description, and `FromKey` already matches case-insensitively. The agent correctly made no change, and Publish-AgentPr correctly reported an empty diff. The fault is in how I designed the issue: I did not check whether the feature already existed. The skill had no "already done" exit, so the agent got PUBLISH_FAIL.
- **u05 outlier (622k, 50% of the round).** `WorkOrderCountByStatusQuery`, its handler and its validator already exist, which is the same issue-design fault. The issue named no types, so `-Issue` printed NO_TYPES. The agent then made 12 Get-Context calls guessing names, and each one reprinted the issue text. It made 16 reads/searches, then added a duplicate `WorkOrderCountForStatus*` set. Reads before the first edit came to 443.0k (71% of the run).
- **u03:** Qodana required `ClickAsync` in a bUnit test, which caused a CI RED, a second submit, and 4 waits.
- **u02:** the second wait was cut off by the terminal timeout (192 s, result null) and run again.

## Attribution (all 8)
| Source | Tokens | Share |
|---|---|---|
| Reads/search before first edit (incl. issue text) | 821.0k | 66.2% |
| Assistant msgs/tool args | 214.9k | 17.3% |
| Skill file | 108.3k | 8.7% |
| Edits | 42.5k | 3.4% |
| Prompt | 25.1k | 2.0% |

Wait-call turns replay 167.7k (13.5%). There were 0 get_mcp_tools calls, down from 7 in round 3; the explicit prompt line worked. There were no real SCRIPT_FAILs and no RISK=FIX.

## Get-Context -Issue evaluation
It works when the issue names a type: u02 and u04 needed 1 call each, and those were the two cheapest runs. It fails badly when the issue names no type (u05, u06), and the rule "at most 2 calls" was ignored (6-12 calls). Its retries also reprinted the issue every time.

## v18g fixes
1. Get-Context prints the issue only on the first call. It adds `EXISTING=` (production files whose names contain 2+ issue keywords; this would have shown the u05 query and handler) and falls back to those files when no type is named. A backticked `Type.NewMember` now outlines `class Type`.
2. Skill: "never a third Get-Context call", plus `RESULT=ALREADY_DONE <file:line>` and "never add a duplicate type". On an empty diff, Submit prints NO_CHANGES (exit 0) instead of PUBLISH_FAIL.
3. Wait cost: Submit now waits inline for whatever is left of an 8-minute budget (Submit takes ~1 min), so Submit plus the first wait is one turn. This saves a full context replay per run. v14g broke because it added a full 9-minute wait on top of Submit.
4. Test-ChangeRisk flags sync bUnit `.Click(` / `.Change(` / `.Input(` / `.Submit(` in tests, and the bUnit trap is added to the skill.
5. Issue design: before creating round-5 issues, I check each target against the code so it does not already exist.
