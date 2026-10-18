# Phase 6 round 16 (v29g, 6e93cd0b): p01-p08 (-v29), unseen kinds

Measured exactly as prior rounds (`measure_transcript.py` logic, `attribute_transcript_tokens.py`; bytes/4). 8/8 green.

| Item | Kind | Issue | PR | Transcript | Input | Calls | Get-Context calls (KB) | Shell reads | Submits | Waits |
|---|---|---|---|---|---|---|---|---|---|---|
| p01 | state-command permission | #10509 | 10518 | bc-ac7c1607 | 37.9k | 9 | 4 (15.9) | 0 | 1 | 1 |
| p02 | HTTP header (ping) | #10510 | 10517 | bc-ee409b3c | 56.4k | 11 | 5 (17.8) | 0 | 1 | 1 (+1 MCP call after GREEN, 7.8k) |
| p03 | health check data | #10511 | 10521 | bc-99b52356 | 157.5k | 19 | 7 (25.5, LIMIT) | 0 | 2 (CI RED) | 3 |
| p04 | CSV parser error | #10512 | 10520 | bc-84167732 | 67.7k | 12 | 5 (18.0) | 0 | 1 | 2 (1 empty) |
| p05 | nav menu link | #10513 | 10522 | bc-81fef3f1 | 227.4k | 25 | 8 (23.3, LIMIT x2) | 2 | 4 (build, test, CI RED) | 3 |
| p06 | logging | #10514 | 10524 | bc-f1d07590 | 210.1k | 20 | 8 (37.7, LIMIT) | 1 | 2 (CI RED) | 3 |
| p07 | utility class | #10515 | 10519 | bc-2cc1c8da | 36.2k | 10 | 5 (12.2) | 0 | 1 | 1 |
| p08 | search spec filter | #10516 | 10523 | bc-2a0fe22f | 100.2k | 16 | 7 (18.0, LIMIT) | 2 | 1 | 1 |
| **Total** | | | | | **893.3k** | 122 | 49 (168.4 KB) | 5 | 13 | 15 |

- **Per run:** 111.7k avg; **median 84.0k**; max 227.4k (p05); min 36.2k (p07).
- **Output:** 9.72k.

## Comparison
| | Per run | Median | Round 16 vs it |
|---|---|---|---|
| Round 15 validation (v28g, u-set) | 83.7k (51.5k w/o u08) | 47.6k | avg +33%, median +76% |
| Round 14 (v28g, familiar items) | 30.0k | | +272% |

The unbounded-read blowup is gone (largest single read 7.9 KB vs u08's 87.4 KB grep). But every run now pays for 4-8 Get-Context calls, and three CI REDs added full Submit + wait cycles.

## Attribution (all 8)
| Source | Tokens | Share |
|---|---|---|
| Reads/search before first edit (Get-Context) | 521.6k | 58.4% |
| Assistant msgs/tool args | 152.8k | 17.1% |
| Skill file | 91.9k | 10.3% |
| Edits | 61.4k | 6.9% |
| Prompt | 25.7k | 2.9% |
| Wait + submit results | 29.5k | 3.3% |

Wait-call turns replay 170.0k (19.0%).

## Top cost drivers
1. **The 6-call budget became a target.**
   - 49 Get-Context calls (6.1 per run, 2.6-5.3 KB each), against 13 in round 15. 4 runs hit LIMIT. 3 then fell back to shell `cat`/`sed`/`head` (rule bypass).
   - Most follow-up calls were `-Find` hunts for *how tests are set up*: a capturing logger (p06: 5 Finds), a DbContext for a health check (p03: 5), NavMenu bUnit rendering (p05), search-filter integration tests (p08).
   - `-Find` returns file outlines, not the matching lines, so each answer was 3-5 KB and often missed.
   - p08 passed `-File` and `-Find` together; only `-File` runs.
2. **CI REDs and test failures (p03, p05, p06), each a full extra Submit + wait cycle of 40-90k:**
   - p06: Qodana RedundantUsingDirective on a non-System using. The risk check only knows the System.* implicit usings.
   - p03: a new health-check test failed on Windows LocalDB only (provider-specific assertion).
   - p05: build error, then a unit test failure. Then CI RED because an existing acceptance test (`FetchData_NavLink_ShouldNotBeVisible`) contradicted the issue. The agent found it via shell reads.
3. **Waits:** 3 Acceptance flake reruns plus 1 empty wait. Wait turns replay 19%.

## v30 proposal (generic)
1. **Back to a 3-call Get-Context limit** (issue + 2), with `-Find` returning **matching lines ±2 context, max 25 hits / 1500 chars** instead of outlines. One call then answers "how is X done" without pulling whole outlines. Reject `-File` + `-Find` in one call.
2. **Print a `TEST SETUP:` line in the first call:** the test helpers in the sibling test project that the target's tests would use (classes named `*Logger*`, `Stub*`, `TestHost`, `*Fixture*` that the sibling test file references). Also print 1 existing test of the same target type. That covers the logger/DbContext/bUnit hunts.
3. **Existing tests that mention the changed element:** the first call lists acceptance/unit test names that reference the target's data-testid or member (p05's contradicting test would have shown up before the Submit).
4. **Risk check:** for each added `using X;`, flag RedundantUsing when no identifier from namespace X's types appears in the file. A cheap heuristic: the last segment of X, or any type that rg finds declared in namespace X, is absent from the file. This generalizes p06 and round 12's s05.
5. **Skill:** "After LIMIT, edit; never shell-read" is stated once (no new size).
