# Phase 6 round 10 (v23g, b54c71a5): v01-v08 (k10)

Measured with `measure_transcript.py` + `attribute_transcript_tokens.py`, bytes/4. 8/8 green, no outlier.

| Item | Type | Issue | PR | Input | Output | Calls | Get-Context | Reads (excl. skill) | Submits | CI RED |
|---|---|---|---|---|---|---|---|---|---|---|
| v01 | domain | #10397 | 10407 | 38.2k | 0.67k | 11 | 2 | 3 | 1 | 0 |
| v02 | UI/bUnit | #10398 | 10412 | 129.8k | 2.71k | 14 | 2 | 4 | 2 | 0 |
| v03 | MCP | #10399 | 10405 | 84.3k | 0.74k | 12 | 2 | 3 | 1 | 0 |
| v04 | query | #10400 | 10410 | 108.9k | 0.91k | 19 | 2 | 7 | 2 | 0 |
| v05 | API JSON | #10401 | 10411 | 93.7k | 1.38k | 17 | 2 | 4 | 2 | 0 |
| v06 | validation | #10402 | 10409 | 57.3k | 0.64k | 15 | 2 | 7 | 1 | 0 |
| v07 | DB column | #10403 | 10406 | 73.8k | 1.00k | 15 | 1 | 2 | 2 | 0 |
| v08 | bug fix | #10404 | 10408 | 35.8k | 0.46k | 12 | 1 | 4 | 1 | 0 |
| **Total** | | | | **622.0k** | **8.51k** | 115 | 14 | 34 | 12 | 0 |

Per run: **77.7k**. Max run 129.8k, the lowest maximum of any Phase 6 round. There were no CI REDs. The 4 extra submits were local catches (2 BUILD=FAIL, 2 RISK), each fixed in one edit.

## Comparison (input per run)
| Reference | Per run | Round 10 (77.7k) vs it |
|---|---|---|
| Round 9 (v22g) | 245.1k | -68% |
| Round 8 (v21g) | 364.6k | -79% |
| Round 7 (v20g) | 146.8k | -47% |
| Round 6 (v19g) | 65.5k | +19% (round 6 had no multi-file kinds like DB/API) |
| Original baseline | 392k | -80% |

Same kind of work, round 9 -> round 10: MCP 399.1k -> 84.3k, query 229.8k -> 108.9k, API 937.0k -> 93.7k, DB column 216.7k -> 73.8k, validation 23.8k -> 57.3k, bug fix 21.6k -> 35.8k.

## Attribution (all 8)
| Source | Tokens | Share |
|---|---|---|
| Reads/search before first edit (incl. Get-Context) | 315.5k | 50.7% |
| Assistant msgs/tool args | 127.3k | 20.5% |
| Skill file | 72.6k | 11.7% |
| Edits | 50.8k | 8.2% |
| Prompt | 22.7k | 3.7% |
| Submit output | 19.9k | 3.2% |

Wait-call turns replay 87.4k (14.1%). get_mcp_tools: 2 calls (1.9k).

## v23g evaluation
- **Read budget (4 before first edit):** mostly followed. Reads fell from 91 to 34 in total. 6 of 8 runs stayed within 4; v04 and v06 used 7. No run opened docs/ or scripts/.
- **New recipes:** `db-column` made v07 the cheapest DB change so far (1 Get-Context call, 2 reads). `api-json-field` brought the API item to 93.7k. The existing MCP and query recipes now work with the read budget (84.3k / 108.9k).
- **Bare-type mapping and init-only risk:** v07 got Employee.cs FULL. The init-only RISK fired locally, so the Qodana CI RED from round 9 w07 did not recur.

## v02 diagnosis (129.8k, 2.71k output)
The counter page and its test were both FULL in the first Get-Context call. The agent then **rewrote both files whole through a PowerShell here-string**. FULL output leaves out `using` lines, so the rewritten test lost its usings (IUiBus/IBus) and the build failed. It then made a second Get-Context call, 3 greps (5.2 KB and 7.4 KB) to find which namespaces IUiBus/IBus come from, and read a sibling test. The large output (2.71k) comes from writing both full files as command arguments.

## v24g
1. FULL output keeps usings, on one compact line (`usings: Bunit; ClearMeasure.Bootcamp.Core; ...`).
2. Skill: make edits with the edit tool, never rewrite files through the shell, and keep usings (2,281 bytes).
