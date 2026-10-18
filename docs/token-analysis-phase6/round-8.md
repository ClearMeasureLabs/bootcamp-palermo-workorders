# Phase 6 round 8 (v21g, e907ebcd): z01-z08

Measured with `measure_transcript.py` + `attribute_transcript_tokens.py`, bytes/4. 8/8 green.

| Item | Issue | PR | Input | Output | Calls | Get-Context | Reads | Submits | Waits |
|---|---|---|---|---|---|---|---|---|---|
| z01 | #10365 | 10373 | 50.1k | 0.65k | 12 | 2 | 6 | 1 | 1 |
| z02 | #10366 | 10375 | 38.0k | 1.26k | 9 | 2 | 1 | 1 | 2 |
| z03 | #10367 | 10379 | 33.3k | 0.52k | 9 | 2 | 1 | 1 | 1 |
| z04 | #10368 | 10380 | 2,473.1k | 7.52k | 81 | 3 (LIMIT) | 38 | 7 | 1 |
| z05 | #10369 | 10374 | 195.4k | 1.05k | 21 | 2 | 12 | 1 | 1 |
| z06 | #10370 | 10378 | 32.1k | 0.52k | 9 | 2 | 2 | 1 | 1 |
| z07 | #10371 | 10376 | 28.9k | 0.50k | 8 | 2 | 2 | 1 | 1 |
| z08 | #10372 | 10377 | 65.7k | 0.66k | 12 | 2 | 6 | 1 | 1 |
| **Total** | | | **2,916.6k** | **12.68k** | 161 | 17 | 68 | 14 | 9 |

Per run: **364.6k**. z04 alone is 85% of the round. Without z04: 443.5k / 7 = **63.4k**.

## Comparison (input per run)
| Reference | Per run | Round 8 |
|---|---|---|
| Round 6 (v19g) | 65.5k | +457% all; -3% w/o z04 |
| Round 7 (v20g) | 146.8k (51.4k w/o 2 outliers) | +148% all |
| Original baseline | 392k | -7% all |

## Attribution (all 8)
| Source | Tokens | Share |
|---|---|---|
| Reads/search before first edit | 1,423.1k | 48.8% |
| Assistant msgs/tool args | 532.5k | 18.3% |
| Reads/search after first edit | 515.6k | 17.7% |
| Edits | 272.3k | 9.3% |
| Skill file | 85.8k | 2.9% |
| Submit output | 53.8k | 1.8% |

## z04 diagnosis (2,473k)
1. Its 3 RISK=FIX rounds were legitimate CRAP warnings (ListWorkOrders CC 6->8, WorkOrderQueryFilters.Apply 8->10). Each took about 0 s, but each added edits and turns.
2. **The root cause is a script bug.** `Invoke-AgentBuildTest.ps1` ran `dotnet test -v q`, and that hides each failing test's name and message. v19g's "show the assertion" filter therefore had nothing to match, and Submit printed only `TEST=FAIL`. The agent resubmitted blind 3 times, then spent about 45 turns working out the failure: it read RemotableRequestTests 3x, LlmGateway tests, WebServiceMessage, ObjectMother, and /tmp files, started Docker, and ran the script with its own filters. Everything after the first edit replayed a large context.
3. **The actual failure:** the new `DueBefore` (DateOnly) property on the remotable `WorkOrderSpecificationQuery` was filled by AutoFaker, which broke `RemotableRequestTests.ShouldSerialize`. WorkOrder.DueDate already has a skip rule (`ObjectMother.WithSkip<WorkOrder>(wo => wo.DueDate)`), and the agent eventually added the same for DueBefore.

## v21g evaluation
- **Recipes:** printed in z04 and z05. z05 (new query) cost 195.4k, down from 489.5k for y05 in round 7 (-60%), but it still read 12 files. These were look-alike Employee handlers and tests, even though the recipe was complete.
- **2-call LIMIT:** worked. There were 17 Get-Context calls (25 in round 7). It fired once (z04) and the agent complied. But 7 of 8 runs used the second call, at about 6 KB each.
- **Typical runs:** z01-z03 and z06-z08 came in at 28.9-65.7k.

## v22g (targets the failure-blind digging, about 2M tokens this round)
1. `Invoke-AgentBuildTest.ps1` and Submit's integration-test run add `--logger 'console;verbosity=minimal'`. The parser prints each `Failed <test>`, its error message (up to 8 lines) and only the stack frames in `src/`. If nothing parses, it prints the last 15 lines. Tested on the box with two failing Shouldly tests: names, expected/actual values and file:line were all printed.
2. Skill: "A FAIL that names nothing -> SCRIPT_FAIL, never dig". The remoting trap now names ObjectMother/BogusOverrides and the DateOnly rule.
3. Follow-up `-Find` calls are capped at 3 KB (was 5.5 KB).
