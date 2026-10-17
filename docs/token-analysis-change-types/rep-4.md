# Rep 4 (v8: flaky acceptance/integration auto-rerun, post-rerun grace, -ForceRerun, NO_CHANGE notice)

19/19 green, every PR a single src-only commit, no failures. Estimates in tokens (bytes/4), same method. The rep-3 total includes the failed t08 first try.

| Type | Rep4 PR | Input r1 | r2 | r3 | r4 | r1→r4 | Output r1 | r2 | r3 | r4 | r1→r4 | r4 calls |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| t01 | #10151 | 1,814k | 381k | 1.8k | 1.9k | -100% | 7.24k | 2.47k | 0.52k | 0.52k | -93% | 3 |
| t02 | #10150 | 700k | 117k | 86k | 5.8k | -99% | 3.17k | 1.91k | 2.61k | 0.56k | -82% | 4 |
| t03 | #10149 | 1,142k | 290k | 1.9k | 1.8k | -100% | 6.72k | 2.94k | 0.55k | 0.51k | -92% | 3 |
| t04 | #10152 | 3,811k | 1,107k | 1.8k | 1.9k | -100% | 9.13k | 3.97k | 0.51k | 0.52k | -94% | 3 |
| t05 | #10158 | 1,432k | 768k | 1.9k | 5.8k | -100% | 7.12k | 4.01k | 0.52k | 0.56k | -92% | 4 |
| t06 | #10157 | 1,158k | 191k | 3.0k | 1.8k | -100% | 5.37k | 2.54k | 0.63k | 0.51k | -91% | 3 |
| t07 | #10153 | 127k | 69k | 1.9k | 1.8k | -99% | 1.55k | 1.70k | 0.54k | 0.50k | -68% | 3 |
| t08 | #10156 | 102k | 56k | 1.8k | 5.8k | -94% | 1.59k | 1.14k | 0.50k | 0.57k | -64% | 4 |
| t09 | #10155 | 1,927k | 656k | 544k | 5.8k | -100% | 7.64k | 3.73k | 4.54k | 0.56k | -93% | 4 |
| t10 | #10154 | 388k | 162k | 1.9k | 2.2k | -99% | 3.02k | 2.50k | 0.54k | 0.63k | -79% | 3 |
| t11 | #10159 | 601k | 483k | 144k | 3.1k | -99% | 3.58k | 2.44k | 3.59k | 0.66k | -81% | 4 |
| t12 | #10160 | 528k | 249k | 3.0k | 1.9k | -100% | 2.59k | 2.70k | 0.65k | 0.53k | -80% | 3 |
| t13 | #10161 | 564k | 145k | 1.8k | 1.9k | -100% | 3.62k | 2.23k | 0.52k | 0.53k | -85% | 3 |
| t14 | #10162 | 73k | 103k | 1.8k | 4.1k | -94% | 1.72k | 1.59k | 0.51k | 0.70k | -59% | 5 |
| t15 | #10163 | 775k | 330k | 2.9k | 2.0k | -100% | 3.48k | 2.63k | 0.63k | 0.58k | -83% | 3 |
| t16 | #10167 | 1,236k | 2,135k | 13k | 13k | -99% | 5.04k | 8.51k | 0.82k | 0.68k | -87% | 7 |
| t17 | #10166 | 1,270k | 156k | 5.8k | 5.9k | -100% | 5.74k | 1.92k | 0.56k | 0.59k | -90% | 4 |
| t18 | #10165 | 65k | 35k | 2.0k | 6.7k | -90% | 1.52k | 1.19k | 0.55k | 0.79k | -48% | 6 |
| t19 | #10164 | 88k | 14k | 2.9k | 1.8k | -98% | 2.06k | 0.85k | 0.62k | 0.49k | -76% | 3 |
| **total** | | **17.80M** | **7.44M** | **824k** | **74.5k** | **-99.6%** | **81.9k** | **51.0k** | **20.3k** | **11.0k** | **-87%** | |

**Rep 4 vs rep 1:** input -99.6%, output -87%. **vs rep 2:** -99.0% / -78%. **vs rep 3:** -91% / -46%.

## Reruns
- **Automatic rerun:** fired once. On t14 (PR #10162) the CI Build run shows `run_attempt=2` (success). The wait script handled it with no agent action; it cost 2 extra wait calls (5 calls total).
- **Every other PR:** attempt 1, green.
- **`-ForceRerun`:** never used. No agent edited unrelated tests (v8's target for t09/t11).

## Why the ~9-11 KB runs took extra calls
- **`get_mcp_tools` habit (t02, t05, t08, t09, t16, t17, t18):** one discovery call at the start, though the prompt says no MCP. It adds ~4k input each, because its result enlarges every later turn.
- **A third wait call (t11, t14, t18):** CI ran longer than the 8-min plus second-call window; t14 also includes the rerun's extra CI cycle.
- **t16:** 3 `todo_write` calls.
- **t18:** after the RESULT, a stray 0-s wait call plus `get_mcp_tools` and `read_file`.

## v9 decision
No script change. The only clear win is a prompt sentence banning `get_mcp_tools`/`todo_write` (~4k per affected run). That is behavioural, and it would confound the stability rep, so v8 stays unchanged for rep 5.
