# Phase 6 round 9 (v22g, 8e294bbb): w01-w08 (k9)

Measured with `measure_transcript.py` + `attribute_transcript_tokens.py`, bytes/4. 8/8 green.

| Item | Type | Issue | PR | Input | Output | Calls | Get-Context | Reads | Submits | CI RED |
|---|---|---|---|---|---|---|---|---|---|---|
| w01 | domain | #10381 | 10389 | 61.4k | 0.91k | 14 | 3 (LIMIT) | 6 | 1 | 0 |
| w02 | UI/bUnit | #10382 | 10392 | 71.4k | 0.72k | 15 | 2 | 8 | 1 | 0 |
| w03 | MCP | #10383 | 10395 | 399.1k | 2.01k | 34 | 2 | 20 | 3 | 0 |
| w04 | query | #10384 | 10394 | 229.8k | 1.85k | 27 | 2 | 17 | 1 | 0 |
| w05 | API | #10385 | 10396 | 937.0k | 3.90k | 45 | 4 (2 LIMIT) | 29 | 3 | 1 |
| w06 | validation | #10386 | 10390 | 23.8k | 0.84k | 9 | 3 (LIMIT) | 1 | 1 | 0 |
| w07 | DB column | #10387 | 10393 | 216.7k | 1.54k | 24 | 2 | 8 | 3 | 1 |
| w08 | bug fix | #10388 | 10391 | 21.6k | 0.43k | 8 | 1 | 2 | 1 | 0 |
| **Total** | | | | **1,960.6k** | **12.20k** | 176 | 19 | 91 | 14 | 2 |

Per run: **245.1k**. The four single-file runs (w01, w02, w06, w08) averaged **44.6k**. The four multi-file kinds (MCP, query, API, DB) took 90%.

## Comparison (input per run)
| Reference | Per run | Round 9 (245.1k) vs it |
|---|---|---|
| Round 6 (v19g) | 65.5k | +274% |
| Round 7 (v20g) | 146.8k | +67% |
| Round 8 (v21g) | 364.6k | -33% |
| Original baseline | 392k | -37% |

## Attribution (all 8)
| Source | Tokens | Share |
|---|---|---|
| Reads/search before first edit | 1,339.6k | 68.3% |
| Assistant msgs/tool args | 339.9k | 17.3% |
| Skill file | 100.1k | 5.1% |
| Edits | 76.6k | 3.9% |
| get_mcp_tools | 19.5k | 1.0% |

Wait-call turns replay 252.6k (12.9%).

## v22g evaluation
- **Test-failure details work.** w05's local failure printed the test name, the Shouldly expected/actual values and `ApiVersioningEndpointTests.cs:line 68`, and the agent fixed it in one edit. There was no blind resubmit or digging after a failure this round (z04 in round 8 had 3 blind resubmits and about 45 turns of digging).
- **No-dig rule:** it was not needed, because no FAIL lacked detail. CI REDs (Qodana) were handled by reading the named item.
- **3 KB `-Find` cap** held.

## w05 diagnosis (937.0k, 48% of the round)
My issue design was ambiguous. `TimeController` returns **plain text** (ISO instant, ETag), so "response gains `dayOfWeek`" meant changing the response format. Get-Context gave TimeController in full on the first call, but the agent then made 29 reads surveying how other controllers build JSON (Version, Diagnostics, ToolsRandom, TimestampConverter, WordCount, ConditionalGetEtag 3x, a 26 KB grep, rate-limit and versioning tests). It even opened `docs/token-analysis-phase6/PLAN.md`. Switching to JSON broke a content-type test (shown with full details, fixed in one edit). It then left `ConditionalGetEtag.CreateWeakEtagForPlainText` unused, and CI failed on Qodana UnusedMember. It also ran the same Get-Context call twice.

## 60-80 KB runs
- **w03 MCP limit (399.1k):** the MCP recipe printed, but the agent made 20 reads (WorkOrderTools 3x, McpWorkOrderToolTests 3x, TestHost, DatedWorkOrderScheduling, ToolsGuidGenerator for a 1-100 range pattern). It had 1 RISK resubmit. Two wait calls came back null, after which it called `get_mcp_tools` and read `terminals/*.txt` files.
- **w04 overdue query (229.8k):** the query recipe printed, but the agent made 17 reads on how DueDate and status filtering are applied (WorkOrderQueryFilters, specification handler tests, RemotableRequestTests, BogusOverrides, other validators).
- **w07 phone column (216.7k):** the issue named no new type, so the look-alike fallback picked junk (LocalStorageUserSessionStore, from "store"/"number"). The agent read EmployeeMap, the 032 script and the mapping tests by hand. Then CI failed on Qodana `PropertyCanBeMadeInitOnly.Global` for the new `PhoneNumber { get; set; }`, which needed a second CI cycle.
- **Common pattern:** even with a recipe, agents keep reading "just to be sure". Nothing limits reads before the first edit. The script-enforced Get-Context limit works (it fired 4 times and agents complied), but plain-text rules (the read limit) have not been followed reliably.

## v23g (targets reads before first edit, 68%)
1. **Read budget in the skill:** at most 4 reads/greps before the first edit, never open docs/ or scripts/, and "unsure? edit anyway, Submit and CI name what's missing" (failure details now make that safe). Also: keep an endpoint's response format unless the criteria say otherwise.
2. **New recipes:** `db-column` (032 script + EmployeeMap 14-30 + EmployeeMappingTests 58-78) and `api-json-field` (ToolsWordCountController + first 40 lines of its tests).
3. **Get-Context:** a backticked bare type (`` `Employee` ``) maps to `class Employee`, and more stopwords are filtered out of EXISTING. For w07 it now prints the db-column recipe plus Employee.cs FULL (5.1 KB, 1 call).
4. **Test-ChangeRisk:** QODANA_RISK for a new Core/Model `{ get; set; }` without the PropertyCanBeMadeInitOnly line (checked on the box with a temporary PhoneNumber property). The skill traps add "leave no unused members".
