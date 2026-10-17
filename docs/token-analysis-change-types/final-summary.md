# Phase 3 final summary: 19 change types × 5 reps

**Method:** estimates from cloud-agent transcripts (bytes/4).
- **Input** = conversation replayed at each tool-call turn, summed. The fixed system/tool overhead per turn is not counted.
- **Output** = reasoning + text + tool args.

Every green PR touched only `src/` and was left open for Jeffrey's cleanup decision.

## Totals per rep
| Rep | Scripts | Green | Input | vs rep 1 | vs previous rep | Output | vs rep 1 | vs previous rep |
|---|---|---|---|---|---|---|---|---|
| 1 | v6 (general only) | 18/19 (t16 PUBLISH_FAIL) | 17,801k | - | - | 81.9k | - | - |
| 2 | v6.1 (worktree publish, full unit suite, 8-min waits, file maps + traps) | 19/19 | 7,445k | -58% | -58% | 51.0k | -38% | -38% |
| 3 | v7/v7.1 (templates, CRAP/Qodana output, push retry) | 19/19 (+1 failed t08 try, push rejected) | 824k | -95% | -89% | 20.3k | -75% | -60% |
| 4 | v8 (flaky rerun, grace, -ForceRerun) | 19/19 | 75k | -99.6% | -91% | 11.0k | -87% | -46% |
| 5 | v8 unchanged (stability) | 19/19 | 106k | -99.4% | +42% | 12.0k | -85% | +9% |

## Per type
| Type | Rep5 PR | Input r1 | r2 | r3 | r4 | r5 | r1→r5 | Output r1 | r2 | r3 | r4 | r5 | r1→r5 | r5 calls |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| t01 | #10196 | 1,814k | 381k | 1.8k | 1.9k | 1.9k | -99.9% | 7.24k | 2.47k | 0.52k | 0.52k | 0.53k | -93% | 3 |
| t02 | #10195 | 700k | 117k | 86k | 5.8k | 1.8k | -99.7% | 3.17k | 1.91k | 2.61k | 0.56k | 0.50k | -84% | 3 |
| t03 | #10194 | 1,142k | 290k | 1.9k | 1.8k | 2.0k | -99.8% | 6.72k | 2.94k | 0.55k | 0.51k | 0.58k | -91% | 3 |
| t04 | #10191 | 3,811k | 1,107k | 1.8k | 1.9k | 59k | -98% | 9.13k | 3.97k | 0.51k | 0.52k | 2.11k | -77% | 16 |
| t05 | #10190 | 1,432k | 768k | 1.9k | 5.8k | 7.0k | -99.5% | 7.12k | 4.01k | 0.52k | 0.56k | 0.85k | -88% | 6 |
| t06 | #10187 | 1,158k | 191k | 3.0k | 1.8k | 1.9k | -99.8% | 5.37k | 2.54k | 0.63k | 0.51k | 0.52k | -90% | 3 |
| t07 | #10192 | 127k | 69k | 1.9k | 1.8k | 1.9k | -99% | 1.55k | 1.70k | 0.54k | 0.50k | 0.52k | -67% | 3 |
| t08 | #10189 | 102k | 56k | 1.8k | 5.8k | 5.8k | -94% | 1.59k | 1.14k | 0.50k | 0.57k | 0.57k | -64% | 4 |
| t09 | #10188 | 1,927k | 656k | 544k | 5.8k | 1.9k | -99.9% | 7.64k | 3.73k | 4.54k | 0.56k | 0.54k | -93% | 3 |
| t10 | #10193 | 388k | 162k | 1.9k | 2.2k | 1.9k | -99.5% | 3.02k | 2.50k | 0.54k | 0.63k | 0.52k | -83% | 3 |
| t11 | #10197 | 601k | 483k | 144k | 3.1k | 1.8k | -99.7% | 3.58k | 2.44k | 3.59k | 0.66k | 0.51k | -86% | 3 |
| t12 | #10198 | 528k | 249k | 3.0k | 1.9k | 1.9k | -99.6% | 2.59k | 2.70k | 0.65k | 0.53k | 0.52k | -80% | 3 |
| t13 | #10199 | 564k | 145k | 1.8k | 1.9k | 3.7k | -99.3% | 3.62k | 2.23k | 0.52k | 0.53k | 0.57k | -84% | 4 |
| t14 | #10200 | 73k | 103k | 1.8k | 4.1k | 2.9k | -96% | 1.72k | 1.59k | 0.51k | 0.70k | 0.61k | -64% | 4 |
| t15 | #10203 | 775k | 330k | 2.9k | 2.0k | 3.7k | -99.5% | 3.48k | 2.63k | 0.63k | 0.58k | 0.55k | -84% | 4 |
| t16 | #10201 | 1,236k | 2,135k | 13k | 13k | 1.8k | -99.9% | 5.04k | 8.51k | 0.82k | 0.68k | 0.49k | -90% | 3 |
| t17 | #10202 | 1,270k | 156k | 5.8k | 5.9k | 1.9k | -99.8% | 5.74k | 1.92k | 0.56k | 0.59k | 0.53k | -91% | 3 |
| t18 | #10204 | 65k | 35k | 2.0k | 6.7k | 1.8k | -97% | 1.52k | 1.19k | 0.55k | 0.79k | 0.49k | -68% | 3 |
| t19 | #10205 | 88k | 14k | 2.9k | 1.8k | 1.8k | -98% | 2.06k | 0.85k | 0.62k | 0.49k | 0.51k | -75% | 3 |
| **total** | | **17,801k** | **7,445k** | **824k** | **75k** | **106k** | **-99.4%** | **81.9k** | **51.0k** | **20.3k** | **11.0k** | **12.0k** | **-85%** | |

## Failures (honest count)
- **Rep 1 t16:** PUBLISH_FAIL; the old publish trusted an analysis-based branch. Fixed in v6.1.
- **Rep 3 t08 first try:** GitHub rejected the push with a bare "(failed)". Fixed in v7.1 with retries; the relaunch was green.
- **CI flakes (unrelated acceptance/integration tests):** rep 3 t02/t09/t11, rep 4 t14 (auto rerun worked), rep 5 t04 (rerun blocked; fixed post-experiment in v9).

## Technique ledger (rep-to-rep, all 19 types)
| Technique | Rep | Δ input | Δ output |
|---|---|---|---|
| v6 general: lean skill, terse RESULT-only prompt, no-wait Invoke, ≤2-min waits, guarded REST publish | 1 | baseline 17,801k | baseline 81.9k |
| v6.1: worktree publish, full unit suite, ≤8-min waits, exact per-type file maps + CI traps | 2 | -58% | -38% |
| v7: canned per-type templates + CRAP/Qodana in wait output + no-digging skill (v7.1 push retry) | 3 | -89% | -60% |
| v8: auto-rerun flaky acceptance/integration jobs, post-rerun grace, -ForceRerun | 4 | -91% | -46% |
| v8 unchanged (stability) | 5 | +42% | +9% |

## Conclusions
1. **Shorter CI waits backfired.** ≤2-min waits multiplied context replays (rep 1). Fewer, longer wait calls plus a publish that never switches branches cut input by 58%.
2. **Canned per-type changes are the dominant lever** (rep 3: −89% input on top of rep 2), as in phase 2. Once a type has a known-green template, the agent's job is two commands.
3. **The remaining cost is CI flakiness, not the change.** Handling flakes in the wait script (rep 4) turned every type one-shot. The single rep-5 outlier came from a rerun edge case, now fixed.
4. **Floor:** ~1.8k input / ~0.5k output per change type (3 calls), against 65k-3.8M input in rep 1.
