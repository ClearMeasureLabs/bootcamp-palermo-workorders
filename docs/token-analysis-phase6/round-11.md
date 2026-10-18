# Phase 6 round 11 (v24g, 1c1a9f39): t01-t08 (k11)

Measured with `measure_transcript.py` + `attribute_transcript_tokens.py`, bytes/4. 8/8 green, every run had 1 wait call and no CI RED.

| Item | Type | Issue | PR | Transcript | Input | Output | Calls | Get-Context | Reads (excl. skill) | Submits |
|---|---|---|---|---|---|---|---|---|---|---|
| t01 | domain | #10413 | 10421 | bc-ffbdc3f5 | 27.3k | 0.55k | 8 | 2 | 1 | 1 |
| t02 | UI/bUnit | #10414 | 10422 | bc-2060c14a | 14.7k | 0.34k | 6 | 1 | 0 | 1 |
| t03 | MCP | #10415 | 10425 | bc-8d9f2b5b | 38.3k | 0.53k | 9 | 2 | 2 | 1 |
| t04 | query | #10416 | 10426 | bc-20c866b2 | 111.9k | 0.82k | 17 | 2 | 7 | 1 |
| t05 | API JSON | #10417 | 10428 | bc-35bff165 | 86.3k | 0.73k | 14 | 2 | 3 | 2 (RISK) |
| t06 | validation | #10418 | 10423 | bc-3dd6267f | 39.2k | 0.70k | 12 | 2 | 5 | 1 |
| t07 | DB column | #10419 | 10427 | bc-df2f52f4 | 100.5k | 0.97k | 18 | 2 | 4 | 3 (2 RISK) |
| t08 | bug fix | #10420 | 10424 | bc-ae98d743 | 20.7k | 0.37k | 8 | 1 | 2 | 1 |
| **Total** | | | | | **438.9k** | **5.01k** | 92 | 14 | 24 | 11 |

Per run: **54.9k**, the best Phase 6 round. t02 (14.7k: 1 Get-Context call, 0 reads) is the cheapest run ever.

## Comparison (input per run)
| Round | Skill | Per run | Round 11 vs it |
|---|---|---|---|
| 10 | v23g | 77.7k | -29% |
| 9 | v22g | 245.1k | -78% |
| 8 | v21g | 364.6k | -85% |
| 7 | v20g | 146.8k | -63% |
| 6 | v19g | 65.5k | -16% |
| original baseline | | 392k | -86% |

## Attribution (all 8)
| Source | Tokens | Share |
|---|---|---|
| Reads/search before first edit (incl. Get-Context) | 247.3k | 56.3% |
| Assistant msgs/tool args | 68.0k | 15.5% |
| Skill file | 58.5k | 13.3% |
| Edits | 38.0k | 8.7% |
| Prompt | 18.2k | 4.1% |

Wait-call turns replay 55.9k (12.7%). get_mcp_tools: 0 calls.

## Did v24g hold?
- **Usings in FULL output:** yes. There were no BUILD=FAIL and no lost-using errors (round 10 v02 had a BUILD=FAIL plus a grep hunt). The UI item (t02) cost 14.7k, against 129.8k for v02.
- **Edit tool only:** yes. No shell here-string rewrites appeared in any transcript.
- **Read budget (still v23g):** 24 reads in total (34 in round 10). Only t04 went over 4 (7).

## Top remaining waste
1. **False CRAP_RISK on EF mappings (t07, about 40k):** `EmployeeMap.Map` was estimated at "CC 12 -> 13" because the estimator counted every lambda `=>` (`entity.Property(e => e.X)`) as a branch. The method passes CI as it is. The agent refactored the mapping into a helper (still flagged at "CC 8"), then refactored again: 2 extra submits and 2 extra reads. t05's CRAP_RISK (ToolsHashController.Post CC 6 -> 8) looked real; one fix.
2. **t04 (111.9k):** the query recipe printed, but for a Role query the agent read Employee look-alikes (handler, validator, Role.cs, 5.9 KB handler tests, 7.1 KB grep). It used 7 reads, over the budget.
3. **Skill file (13.3%) and wait turns (12.7%)** are now structural (about 2.3 KB read once and replayed; 1 wait per run).

## v25g
- `Test-ChangeRisk.ps1`: lambdas (`x => ...`, `() => ...`, `(a, b) => ...`) no longer add to the estimated CC; switch-expression arms still do. Box check: adding `entity.Property(e => e.JobTitle).HasMaxLength(100)` to EmployeeMap now gives no RISK (v24g flagged it).
