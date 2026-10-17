# Rep 5 (v8 unchanged: stability rep)

19/19 green, every PR a single src-only commit. Estimates in tokens (bytes/4), same method.

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

## Stability vs rep 4
- **Totals:** input 75k → 106k (+42%); output 11.0k → 12.0k (+9%).
- **Excluding t04:** input 47k and output 9.9k, which is below rep 4.
- **Calls:** 13/19 types finished in the minimum 3 calls (Invoke, wait, wait). The median type used 1.9k input.
- **Conclusion:** the canned path is stable at ~1.8-2k input and ~0.5k output per type. Run-to-run variance comes from (a) CI flakes and (b) stray `get_mcp_tools`/`read_file` calls (+2-4k).

## Outliers
- **t04 (58.6k input, 16 calls):**
  - The first CI run was RED on an acceptance test unrelated to the change.
  - v8 should have rerun it automatically, then `-ForceRerun` should have, but neither started a rerun. GitHub shows only attempt 2, which came after the agent deleted the wait-state file by hand.
  - Cause (most likely): `gh run rerun --failed` is refused while other jobs of the run are still in progress. v8 ignored the exit code and recorded the run as already rerun, so the auto rerun and -ForceRerun both became no-ops.
  - The agent then read the skill and the wait script and grepped (5 greps, 2 reads) before clearing the state.
  - **Fixed after the experiment (v9, not used in any rep):** the rerun is recorded only when `gh run rerun` succeeds. It is retried on later polls while jobs are still running, and `RERUN_FAILED` is printed otherwise.
- **t05 (7.0k):** a third wait call returned at once with empty output, so the agent re-ran it in the background and read the terminal file, plus one `get_mcp_tools`. Benign; it cost +5k.
