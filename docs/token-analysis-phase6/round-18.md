# Phase 6 round 18 (v31g, 65871666): r01-r08 (-v31), unseen kinds

Measured as in earlier rounds: `measure_transcript.py` totals, `attribute_transcript_tokens.py` attribution, bytes/4. 8/8 checks green, and **no CI REDs** (round 17 had 3).

| Item | Kind | Issue | PR | Transcript | Input | Output | Calls | Get-Context calls (KB) | Edits | Submits | Waits | Rework / notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| r01 | telemetry record field + mapper | #10541 | 10555 | bc-e8cfbd75 | 67.3k | 1.13k | 13 | 3 (12.2, +1 LIMIT) | 6 | 2 | 1 | TEST=FAIL: the test went through the public `TraceEntry` constructor (the mapper is internal), and the constructor didn't copy `Kind` |
| r02 | LLM gateway guard | #10542 | 10549 | bc-3aad000c | 18.9k | 0.55k | 7 | 2 (6.0) | 2 | 1 | 1 | Final reply empty even though NEXT was printed (see note) |
| r03 | view-model computed property | #10543 | 10553 | bc-ef4c7bae | 18.7k | 0.46k | 7 | 1 (4.8) | 2 | 1 | 1 | 1 MCP call after GREEN (4.1k) |
| r04 | refactor: shared constant | #10544 | 10552 | bc-56d648f4 | 39.7k | 0.53k | 9 | 3 (13.9) | 3 | 1 | 1 | none |
| r05 | enum extension method | #10545 | 10550 | bc-b76cec64 | 18.4k | 0.51k | 7 | 2 (5.8) | 2 | 1 | 1 | none |
| r06 | accessibility markup | #10546 | 10551 | bc-7995cbbc | 26.0k | 0.47k | 7 | 2 (11.7) | 2 | 1 | 1 | none |
| r07 | message handler | #10547 | 10554 | bc-516628bb | 56.1k | 1.13k | 10 | 3 (18.0) | 2 | 1 | 2 (1 empty) | get_mcp_tools after GREEN (8.4k) |
| r08 | extract helper (internal) | #10548 | 10556 | bc-e23bfcff | 69.9k | 0.78k | 13 | 4 (13.3, 1 LIMIT) | 4 | 3 | 1 | BUILD=FAIL: tests couldn't see the internal selector. The agent added InternalsVisibleTo, then the risk check flagged RedundantNameQualifier locally |
| **Total** | | | | | **315.0k** | 5.56k | 73 | 20 (85.7 KB) | 23 | 11 | 9 | |

- **Per run:** 39.4k average, median 32.8k. Max 69.9k (r08), min 18.4k (r05).
- **r02:** the PR does include tests: +12 lines in `src/UnitTests/LlmGateway/LlmGatewayHelperTests.cs` (2 tests: no letters -> true; `"Hello 1"`/`fr` -> false), plus the 1-line guard change. "Need the existing TranslationGuard tests…" was a mid-run message. The run's last assistant turn was empty and came right after the `NEXT: print RESULT=GREEN …` line.

## Comparison
| | Per run | Median | Round 18 vs it |
|---|---|---|---|
| Round 17 (v30g, q-set) | 72.3k | 42.6k | avg -46%, median -23% |
| Round 16 (v29g, p-set) | 111.7k | 84.0k | avg -65%, median -61% |
| Round 15 validation (v28g, u-set) | 83.7k | 47.6k | avg -53%, median -31% |
| Round 14 (v28g, familiar items) | 30.0k | | avg +31% |

## Did the v31g changes fire?
- **The 5 new Qodana checks never fired:** no QODANA_RISK line in any Submit output used them, and CI never reported those rules. Nothing in this round can show whether they saved reruns, because none of the r-set changes wrote those patterns. Zero CI REDs is consistent with the checks but doesn't prove they helped.
- **An older local check did fire and caught a CI RED before push:** RedundantNameQualifier, on r08's `[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(...)]`.
- **The CRAP early-return hint** never triggered (no CRAP_RISK this round).
- **NEXT line:** printed in all 8 runs. 7/8 replied with the RESULT line; r02's reply was empty. Two runs made a tool call after GREEN (r03 MCP 4.1k, r07 get_mcp_tools 8.4k; 12.5k total).

## Where the tokens went
| Phase | Tokens | Share |
|---|---|---|
| Happy path | 238.9k | 76% |
| Local rework after a failed Submit (r01 TEST=FAIL, r08 BUILD=FAIL + RISK) | 76.1k | 24% |
| CI-RED rework | 0.0k | 0% |

| Source (`attribute_transcript_tokens.py`) | Tokens | Share |
|---|---|---|
| reads/search before first edit (mostly Get-Context) | 143.8k | 45.7% |
| skill file | 55.9k | 17.7% |
| assistant msgs/tool args | 52.7k | 16.7% |
| edits | 28.2k | 9.0% |
| prompt | 15.4k | 4.9% |
| submit | 10.8k | 3.4% |
| reads/search after first edit | 7.7k | 2.4% |
| wait results | 0.5k | 0.2% |

## Drivers
1. **Context replay (45.7%).** First Get-Context outputs ran 3.4-9.6 KB, and 6 of 8 runs made 1-2 follow-up calls (up to 6.0 KB each). This output is replayed on every later turn.
2. **Testing internal members (76.1k of rework, both failures).** r08 wrote tests against an `internal` class in a project its tests can't see (BUILD=FAIL). r01 avoided the internal mapper by testing through a constructor that didn't copy the new field (TEST=FAIL). Get-Context never says whether the target is internal or visible to the test project.
3. **The skill file (17.7%)** is read once per run at about 2.9 KB and replayed on every turn.
4. **Tool calls after GREEN (12.5k)** and one empty final reply.

## v32 recommendation
Get-Context should print an `ACCESS` line for the edit target, saying whether it is `internal` and whether its project has InternalsVisibleTo for UnitTests:
- **Visible:** `ACCESS: internal, visible to UnitTests`, so test it directly.
- **Not visible:** `ACCESS: internal, NOT visible: add <InternalsVisibleTo Include="ClearMeasure.Bootcamp.UnitTests" /> to <csproj>`, using the csproj form so no assembly attribute needs qualifying.

This targets both rework cases in this round (76.1k, 24% of input). Also trim the skill file's Never-list line (it costs 55.9k in replay) and add "after RESULT, stop" to the NEXT text. Keep the v31g Qodana checks: they cost nothing when they don't fire.
