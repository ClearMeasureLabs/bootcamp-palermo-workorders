# Rep 2 (v6.1: worktree publish, full unit suite, 8-min waits, exact file maps + CI traps)

19/19 green, all PRs src-only. Estimates (bytes/4), same method as rep 1.

| Type | PR | Rep1 in | Rep2 in | Δ in | Rep1 out | Rep2 out | Δ out | Calls | Waits |
|---|---|---|---|---|---|---|---|---|---|
| t01 | #10075 | 1,814k | 381k | -79% | 7.2k | 2.5k | -66% | 31 | 2 |
| t02 | #10074 | 700k | 117k | -83% | 3.2k | 1.9k | -40% | 20 | 3 |
| t03 | #10081 | 1,142k | 290k | -75% | 6.7k | 2.9k | -56% | 31 | 5 |
| t04 | #10080 | 3,811k | 1,107k | -71% | 9.1k | 4.0k | -56% | 45 | 3 |
| t05 | #10079 | 1,432k | 768k | -46% | 7.1k | 4.0k | -44% | 33 | 5 |
| t06 | #10077 | 1,158k | 191k | -84% | 5.4k | 2.5k | -53% | 28 | 2 |
| t07 | #10073 | 127k | 69k | -46% | 1.5k | 1.7k | +10% | 19 | 6 |
| t08 | #10078 | 102k | 56k | -46% | 1.6k | 1.1k | -29% | 12 | 2 |
| t09 | #10082 | 1,927k | 656k | -66% | 7.6k | 3.7k | -51% | 40 | 2 |
| t10 | #10076 | 388k | 162k | -58% | 3.0k | 2.5k | -17% | 24 | 2 |
| t11 | #10083 | 601k | 483k | -20% | 3.6k | 2.4k | -32% | 27 | 3 |
| t12 | #10084 | 528k | 249k | -53% | 2.6k | 2.7k | +4% | 35 | 2 |
| t13 | #10085 | 564k | 145k | -74% | 3.6k | 2.2k | -38% | 23 | 3 |
| t14 | #10086 | 73k | 103k | +40% | 1.7k | 1.6k | -8% | 19 | 3 |
| t15 | #10087 | 775k | 330k | -57% | 3.5k | 2.6k | -24% | 30 | 3 |
| t16 | #10089 | 1,236k (FAIL) | 2,135k | +73% | 5.0k | 8.5k | +69% | 69 | 4 |
| t17 | #10088 | 1,270k | 156k | -88% | 5.7k | 1.9k | -67% | 19 | 2 |
| t18 | #10090 | 65k | 35k | -45% | 1.5k | 1.2k | -22% | 12 | 3 |
| t19 | #10091 | 88k | 14k | -84% | 2.1k | 0.8k | -59% | 9 | 2 |
| **total** | | **17.80M** | **7.44M** | **-58%** | **81.9k** | **51.0k** | **-38%** | | |

## Remaining cost drivers
1. **t16 webhook (2.13M, 29% of rep-2 input).** Its first Invoke calls returned empty (0-18 s), then CI went red on a **Qodana** finding (an unused `using`). Neither the wait output nor the log showed it, so the agent downloaded and unzipped the Qodana SARIF by hand. That took two CI cycles plus 15 greps and 19 reads.
2. **Digging into earlier runs:** t04 (1.11M) fetched the rep-1 branch and copied files from it; t16 searched old issues and comments. Agents want the known-good change, so the rep-3 templates hand it to them directly.
3. **Gates other than tests:** t05 (768k) failed the **CRAP gate**, which wasn't visible in the wait output, so the agent dug through logs and ran a second CI cycle.
4. **Discovery reads are still there:** 4-19 reads per type, even with exact file maps.
5. **Empty wait outputs** (long calls are recorded empty) lead to manual `gh run list`/`view` calls.

Waits dropped from 5-24 per run (rep 1) to 2-6.
