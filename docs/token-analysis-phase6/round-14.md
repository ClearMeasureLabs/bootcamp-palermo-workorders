# Phase 6 round 14 (v28g, 2fa0a4b7): q01-q08 (k14), confirmation round

Measured with `measure_transcript.py` + `attribute_transcript_tokens.py`, bytes/4. 8/8 green, no CI RED.

| Item | Type | Issue | PR | Transcript | Input | Output | Calls | Get-Context | Reads before edit | Submits | Waits | Calls after GREEN |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| q01 | domain | #10461 | 10470 | bc-577b469b | 18.7k | 0.47k | 7 | 1 | 0 | 1 | 2 (1 empty) | 0 |
| q02 | UI/bUnit | #10462 | 10469 | bc-a1c6130a | 35.0k | 0.42k | 10 | 1 | 0 | 1 | 1 | 3 (15.0k) |
| q03 | MCP | #10463 | 10471 | bc-363df618 | 39.7k | 0.53k | 10 | 2 | 0 (1 read mid-edit) | 1 | 1 | 0 |
| q04 | query | #10464 | 10472 | bc-b300cb29 | 35.7k | 0.54k | 9 | 1 | 0 | 1 | 1 | 0 |
| q05 | API JSON | #10465 | 10476 | bc-c547d2ee | 42.0k | 0.83k | 10 | 1 | 0 | 2 (CRAP_RISK) | 2 (1 empty) | 0 |
| q06 | validation | #10466 | 10473 | bc-ddfefca2 | 12.5k | 0.46k | 6 | 1 | 0 | 1 | 1 | 0 |
| q07 | DB column | #10467 | 10475 | bc-d6c16060 | 41.8k | 0.56k | 11 | 1 | 0 (1 read mid-edit) | 1 | 1 | 0 |
| q08 | bug fix | #10468 | 10474 | bc-3b7c092e | 14.3k | 0.42k | 6 | 1 | 0 | 1 | 1 | 0 |
| **Total** | | | | | **239.7k** | **4.23k** | 69 | 9 | 0 (+2) | 9 | 10 | 3 (15.0k) |

Per run **30.0k**; max **42.0k** (q05); min 12.5k (q06).

## Comparison
| | Total | Per run | Max run | Round 14 vs it |
|---|---|---|---|---|
| Round 13 (v27g) | 266.6k | 33.3k | 73.9k (r07) | -26.9k total, **-10.1%** |
| Round 12 (v25g) | 433.5k | 54.2k | 125.4k (s05) | -45% |
| Original baseline | | 392k | | **-92.3%** |

## Attribution (all 8)
| Source | Tokens | Share |
|---|---|---|
| Reads/search before first edit (incl. Get-Context) | 93.8k | 39.1% (40.6%) |
| Skill file | 48.4k | 20.2% |
| Assistant msgs/tool args | 39.6k | 16.5% |
| Edits | 36.7k | 15.3% |
| Prompt | 14.5k | 6.1% |
| Submit/wait results, later reads, get_mcp_tools | 6.6k | 2.8% |

Wait-call turns replay 49.8k (20.8%).

## Did v28g hold?
- **No calls after RESULT:** mostly. 7 of 8 runs stopped at GREEN. q02 still made 3 calls (get_mcp_tools twice plus an MCP message-queue check), 15.0k, after the environment prompted a "check the message queue" step. That is down from 6 runs and 35.8k in round 13.
- **Whole-word NUMBER_RISK:** no false flag (q07 Department item: 1 Submit).
- **Recipe usings and full EmployeeMap:** q07 made no read before its first edit (r07 made 2). It still read EmployeeMappingTests once mid-edit, because the file uses a TestHost pattern the recipe excerpt (lines 58-78) didn't show.

## Outliers and causes
- **q05 (42.0k):** CRAP_RISK `CountSentences est. CC 0 -> 6`. The risk threshold is > 5 while CI fails > 6, so a CC 6 method would probably have passed CI. It cost 1 extra Submit plus a refactor. Also 1 wait returned no output (terminal cap) and was rerun.
- **q07 (41.8k):** 5 edits plus 1 mid-edit read of EmployeeMappingTests (see above).
- **q03 (39.7k):** a second Get-Context `-Find 'maxResults|Take\('` (searching for a pattern, not a name) plus 1 read of McpEmployeeToolTests to add a `System.Text.Json` using.
- **q02 (35.0k):** the 15.0k after GREEN. Without it, 20.0k.
- **q01:** 1 empty wait rerun (+one replay turn).

## Verdict: flattening
Round 14 still cut 10.1% vs round 13, above the 5% threshold. But the gain came from removing outliers (calls after GREEN 35.8k -> 15.0k; no run over 42k, vs two over 70k). It did not come from cheaper clean runs: q01, q06 and q08 cost 12.5-18.7k, the same as r01, r06 and r08 (11.6-18.9k).

What remains is mostly fixed cost: skill file 20.2%, prompt 6.1%, edits 15.3%, and one wait turn per run (20.8% replay). A clean run is skill + Get-Context + 2-5 edits + Submit + wait, about 12-20k. The remaining avoidable items (q05's borderline CRAP threshold, the q02 environment prompt, rare mid-edit reads) are worth a few percent at most. Further rounds should give low single-digit gains, so it's time to move to the comment-to-descriptive-names experiment, using v28g as the frozen baseline (30.0k/run).
