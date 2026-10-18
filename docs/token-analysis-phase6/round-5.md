# Phase 6 round 5 (v18g, e5fb4d33): w01-w08

Measured with `measure_transcript.py` + `attribute_transcript_tokens.py`, bytes/4. 8/8 green.

| Item | Issue | PR | Input | Output | Calls | Get-Context | Submits | Waits |
|---|---|---|---|---|---|---|---|---|
| w01 | #10317 | 10328 | 45.3k | 0.60k | 11 | 2 | 1 | 1 |
| w02 | #10318 | 10325 | 67.4k | 1.26k | 15 | 2 | 4 | 1 |
| w03 | #10319 | 10329 | 48.8k | 0.51k | 11 | 1 | 1 | 1 |
| w04 | #10320 | 10331 | 200.8k | 1.12k | 23 | 2 | 2 | 1 |
| w05 | #10321 | 10332 | 214.8k | 1.54k | 29 | 2 | 4 | 1 |
| w06 | #10322 | 10326 | 27.8k | 0.44k | 9 | 1 | 1 | 1 |
| w07 | #10323 | 10327 | 20.3k | 0.37k | 8 | 1 | 1 | 1 |
| w08 | #10324 | 10330 | 34.0k | 0.70k | 11 | 1 | 2 | 1 |
| **Total** | | | **659.3k** | **6.54k** | 117 | 12 | 16 | 8 |

Per run: **82.4k**. Six of the eight runs came in at 20-67k (mean 40.6k); w04 and w05 account for 63% of the round.

## Comparison (input per run)
| Reference | Per run | Round 5 (82.4k) vs it |
|---|---|---|
| Round 4 (v17g) | 155k | -47% |
| Round 3 (v16g) | 72k | +14% |
| Round 1 / 2 | 296k / 378k | -72% / -78% |
| Phase 4 rep 3 / Phase 5 | 97k / 99.5k | -15% / -17% |
| Original baseline | 392k | -79% |

## Attribution (all 8)
| Source | Tokens | Share |
|---|---|---|
| Reads/search before first edit | 337.5k | 51.2% |
| Assistant msgs/tool args | 118.1k | 17.9% |
| Skill file | 86.3k | 13.1% |
| Edits | 61.1k | 9.3% |
| Reads after first edit | 22.3k | 3.4% |
| Prompt | 21.1k | 3.2% |
| Submit output | 11.7k | 1.8% |

## Inline CI wait: worked
Every run made exactly **1** separate wait call, compared with 2 per run in round 3 and 18 calls across 8 runs in round 4. The successful Submit calls took 7.2-8.1 min, which is under the terminal cap; every one ended PENDING and was then followed by one WAIT_CMD that returned GREEN. Wait-call turns replayed 59.1k (9.0%), down from 120.4k in round 3 (21%) and 167.7k in round 4 (13.5%).
Bug: in w02, the inline wait after a fix push read the **previous** head's RED (the PR head had not updated yet). The agent then fought a correct `PUBLISH=NO_CHANGE`, making 4 submits.

## EXISTING= / ALREADY_DONE: behaved
EXISTING printed in w01, w03, w05, w06 and w07. There were no false ALREADY_DONE replies and no duplicate types (all targets were verified absent beforehand). In w05 the EXISTING list pointed at the `WorkOrderCountByStatus*` pattern, which the agent copied. Get-Context calls dropped to 12 (36 in round 4), and none of the 8 runs went past the 2-call limit.

## Remaining costs (why w04/w05)
- **w04 (200.8k):** 8 grep/glob calls plus whole-file reads of tests and specification code (`McpWorkOrderToolTests.cs` 6.8 KB, grep 8.2 KB) to work out filtering, after the at-most-2 Get-Context calls. It also hit a CRAP/coverage follow-up (2 submits), and ran `gh pr view` to get the PR URL.
- **w05 (214.8k):** the first 3 Submits failed (output lost to non-zero exits). The unit-test failure showed no assertion text, so the agent ran `dotnet test` by hand, then learned that a new query needs a validator and its test (from a convention test) and read 4 more files.
- **Across runs:** small files (queries, handlers, sibling tests) were read whole right after Get-Context outlined them. Two agents looked up the PR URL themselves.

## v19g (targets reads before first edit, 51%)
1. Get-Context prints files of <= 35 lines in full (marked FULL; the skill says never read them), and lists the `<Name>Tests.cs` sibling of each top production file first. AcceptanceTests are excluded. The cap is 3.5 KB.
2. Submit/Wait always exit 0, so the transcript keeps failure output. Unit failures show `Error Message`/Shouldly lines. Submit prints the full PR URL, and GREEN prints `RESULT=GREEN PR=<url>` ready to copy.
3. Submit waits until the PR head is the commit just pushed before the inline wait (fixes the w02 stale RED). Publish prints `HEAD=`.
4. Traps: a new query/command needs a validator and its test; no `?.`/`??` on non-nullable members (w02 Qodana RED).
