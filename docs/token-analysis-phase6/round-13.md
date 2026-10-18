# Phase 6 round 13 (v27g, e21656b2): r01-r08 (k13)

Measured with `measure_transcript.py` + `attribute_transcript_tokens.py`, bytes/4. 8/8 green, no CI RED, 1 wait per run.

| Item | Type | Issue | PR | Transcript | Input | Output | Calls | Get-Context | Reads before edit | Submits | Calls after GREEN |
|---|---|---|---|---|---|---|---|---|---|---|---|
| r01 | domain | #10445 | 10455 | bc-e313b396 | 18.6k | 0.40k | 7 | 1 | 0 | 1 | 1 (4.1k) |
| r02 | UI/bUnit | #10446 | 10454 | bc-02b1583e | 24.5k | 0.32k | 8 | 1 | 0 | 1 | 2 (9.2k) |
| r03 | MCP | #10447 | 10456 | bc-cd8cba8c | 24.5k | 0.77k | 7 | 1 | 0 | 1 | 1 (5.3k) |
| r04 | query | #10448 | 10460 | bc-61e19f6d | 70.2k | 0.94k | 15 | 1 | 4 | 1 | 1 (7.9k) |
| r05 | API JSON | #10449 | 10458 | bc-5693c595 | 24.4k | 0.72k | 7 | 1 | 0 | 1 | 1 (5.3k) |
| r06 | validation | #10450 | 10453 | bc-fb133dd4 | 11.6k | 0.32k | 6 | 1 | 0 | 1 | 0 |
| r07 | DB column | #10451 | 10459 | bc-4cc1ec37 | 73.9k | 0.98k | 15 | 2 | 2 | 2 (false NUMBER_RISK) | 0 |
| r08 | bug fix | #10452 | 10457 | bc-853337c0 | 18.9k | 0.47k | 7 | 1 | 0 | 1 | 1 (4.1k) |
| **Total** | | | | | **266.6k** | **4.92k** | 72 | 9 | 6 | 9 | 7 (35.8k) |

Per run: **33.3k**, -39% vs round 12 (54.2k). r06 at 11.6k is the cheapest run ever. 6 of 8 runs went skill -> Get-Context -> edits -> Submit -> wait with zero extra reads.

## Comparison (input per run)
| Round | Skill | Per run | Round 13 vs it |
|---|---|---|---|
| 12 | v25g | 54.2k | -39% |
| 11 | v24g | 54.9k | -39% |
| 10 | v23g | 77.7k | -57% |
| 9 | v22g | 245.1k | -86% |
| 8 | v21g | 364.6k | -91% |
| 7 | v20g | 146.8k | -77% |
| 6 | v19g | 65.5k | -49% |
| original baseline | | 392k | -92% |

## Attribution (all 8)
| Source | Tokens | Share |
|---|---|---|
| Reads/search before first edit (incl. Get-Context) | 108.3k | 40.6% (50.5%) |
| Skill file | 49.7k | 18.6% |
| Assistant msgs/tool args | 49.4k | 18.5% |
| Edits | 33.0k | 12.4% |
| Prompt | 14.2k | 5.3% |
| get_mcp_tools | 6.5k | 2.4% |
| Submit + wait results | 5.5k | 2.1% |

Tool calls made after RESULT=GREEN replay **35.8k (13.4%)**: get_mcp_tools (3), todo/`search_replace` no-ops (3), and a handoff-doc write (r04). Wait turns replay 17.9%.

## Did v27g hold?
- **NEW names:** yes. Only 1 second Get-Context call in 8 runs (6 in round 12), and that one (r07) searched for the recipe test, not the new name.
- **Sibling test tail:** yes. There were no test-file re-reads in r01, r03, r05 or r08 (round 12 had 4).
- **Mirror-folder validator test:** yes. r06 made 0 reads (s06 made 4).
- **NEXT_SCRIPT:** yes. r07 created 033 directly, with no glob.
- **Qodana risks:** no Qodana RED this round (2 in round 12). Neither new risk fired, falsely or otherwise.
- **New problem:** a false NUMBER_RISK on `PhoneNumber = "555-0100"` (the regex matched the `Number =` suffix). It cost r07 a second Submit plus 2 edits.

## Top remaining waste
1. **Calls after GREEN: 35.8k (13.4%).** The environment nudges agents toward todos, get_mcp_tools and handoff docs after the work is done.
2. **r07 (73.9k):** the false NUMBER_RISK, plus reading EmployeeMap and EmployeeMappingTests in full, because recipe excerpts hid usings and EmployeeMap was only an excerpt (14-30).
3. **r04 (70.2k):** get_mcp_tools at the start, plus 4 reads/greps on how to filter by `Creator`.
4. **Structural:** skill file 18.6% and prompt 5.3%. Both are now a large share because everything else shrank.

## v28g
- `Test-ChangeRisk.ps1`: NUMBER_RISK only on a whole-word `Number =` (`(?<!\w)`). Box check: `PhoneNumber = "555-0100"` gives no flag; `Number = "12345678"` and `wo.Number = "12345678"` are flagged.
- Recipes print a `usings:` line per file; db-column shows EmployeeMap.cs in full (40 lines).
- GENERAL.md and the launch prompt: after the RESULT line, no more tool calls (no todos, handoff files or get_mcp_tools).
