# Phase 6 round 20: session-size experiment (v33g, e5365956; t-set #10573-#10580)

Design: [session-size-experiment.md](session-size-experiment.md). The same 8 issues ran in three arms on separate branches, with identical prompts apart from packing. **24/24 PRs green**: every check SUCCESS/SKIPPED/NEUTRAL on PRs 10581-10604 per `gh pr view --json statusCheckRollup`, and every session printed all its RESULT lines. No session needed a relaunch.

## Method
- **Input tokens = real cumulative input.** Each model call that issues tool calls re-sends the whole transcript so far, so a call's input is all transcript bytes up to and including that call. A session's input is the sum over its calls, divided by 4 (`measure_transcript.py`, as in every earlier round). In a multi-item session this automatically charges each later item for re-sending all earlier items' context.
- **Per item in B/C:** a call is charged to the item whose `Start-NextItem.ps1` most recently ran. The skill-file read before the first Start-NextItem (1.9-2.1k) is charged to the session's first item.
- **Wall-clock:** first to last `started_at_ms`/`completed_at_ms` in the transcript.

## Result
| Arm | Sessions | Total input (8 items) | Per item avg | vs A | Green | CI REDs (items) | Calls | Output | Wall-clock |
|---|---|---|---|---|---|---|---|---|---|
| A: 1 item/session | 8 | **275.9k** | 34.5k | 1.00x | 8/8 | 2 (t04) | 72 | 3.88k | 11.5-39.5 min each; 135.1 session-min; 39.5 min elapsed (parallel) |
| B: 4 items/session | 2 | **707.9k** | 88.5k | 2.57x | 8/8 | 1 (t04) + 1 SUBMIT=FAIL (t04) | 70 | 2.34k | 72.4 + 62.3 = 134.7 session-min; 72.4 min elapsed |
| C: 8 items/session | 1 | **1,325.7k** | 165.7k | 4.80x | 8/8 | 3 (t03 x2, t04) | 70 | 1.60k | 179.1 min |

### Per item, by arm (input k)
| Item | A | B (session, position) | C (position) |
|---|---|---|---|
| t01 PATCH idempotency | 20.0 | 29.0 (B1, 1) | 31.2 (1) |
| t02 buffer upper bound | 14.9 | 51.0 (B1, 2) | 44.1 (2) |
| t03 JSON omit nulls | 63.0 | 129.8 (B1, 3) | 238.5 (3) |
| t04 Role.ToString | 63.2 | 182.1 (B1, 4) | 156.0 (4) |
| t05 health rounding | 33.8 | 45.0 (B2, 1) | 168.2 (5) |
| t06 read-only role | 12.0 | 47.9 (B2, 2) | 246.0 (6) |
| t07 batch message | 31.3 | 99.8 (B2, 3) | 188.5 (7) |
| t08 envelope factory | 37.7 | 123.2 (B2, 4) | 253.3 (8) |
| **Total** | **275.9** | **707.9** | **1,325.7** |

### Cost by position in the session
| Position | B1 | B2 | B mean | C |
|---|---|---|---|---|
| 1 | 29.0 | 45.0 | 37.0 | 31.2 |
| 2 | 51.0 | 47.9 | 49.5 | 44.1 |
| 3 | 129.8 | 99.8 | 114.8 | 238.5 |
| 4 | 182.1 | 123.2 | 152.7 | 156.0 |
| 5-8 | | | | 168.2, 246.0, 188.5, 253.3 (mean 214.0) |

Position 1 costs about the same as an arm-A run (A mean 34.5k). After that, cost climbs with position, because every call re-sends the earlier items' context, which by the end of C was 150 KB. In C, items 5-8 averaged 214.0k each, 6.2x arm A's per-item average. The same item costs more the later it runs: t08 was 37.7k alone, 123.2k at B2 position 4, and 253.3k at C position 8. Fewer sessions did save fixed overhead (the skill read is about 2k per session, and B/C made 70 calls vs A's 72), but the replay cost dwarfs that. Output tokens also fell slightly (3.88k -> 1.60k), but they're a small fraction of the total.

### CI failures
t04 (`Role.ToString()` returning `Name ?? ""`) tripped Qodana's nullable-contract rules (`Name` is non-nullable) in all three arms: an issue-wording trap, not a session effect. C also had 2 REDs on t03 (NotAccessedPositionalProperty/PropertyCanBeMadeInitOnly in a test record). Packing gave no quality benefit: green rates are equal and REDs didn't fall.

## Recommendation
Run **one work item per session**. Packing 4 items cost 2.6x and packing 8 items cost 4.8x the tokens for the same 8 green PRs, and serial packing also took longer elapsed (72-179 min vs 40 min for parallel single sessions).
