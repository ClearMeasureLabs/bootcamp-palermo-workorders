# Rep 3 (v7/v7.1: per-type templates, CRAP/Qodana in wait output, push retry)

19/19 green, all src-only. Plus one failed first try for t08 (GitHub rejected the push with a bare "(failed)"; v7.1 added retries). Estimates in tokens (bytes/4), same method.

| Type | Rep3 PR | Input r1 | r2 | r3 | r1→r3 | Output r1 | r2 | r3 | r1→r3 | r3 calls |
|---|---|---|---|---|---|---|---|---|---|---|
| t01 | #10111 | 1,814k | 381k | 1.8k | -100% | 7.2k | 2.5k | 0.52k | -93% | 3 |
| t02 | #10119 | 700k | 117k | 86k | -88% | 3.2k | 1.9k | 2.61k | -18% | 19 |
| t03 | #10112 | 1,142k | 290k | 1.9k | -100% | 6.7k | 2.9k | 0.55k | -92% | 3 |
| t04 | #10114 | 3,811k | 1,107k | 1.8k | -100% | 9.1k | 4.0k | 0.51k | -94% | 3 |
| t05 | #10113 | 1,432k | 768k | 1.9k | -100% | 7.1k | 4.0k | 0.52k | -93% | 3 |
| t06 | #10116 | 1,158k | 191k | 3.0k | -100% | 5.4k | 2.5k | 0.63k | -88% | 4 |
| t07 | #10117 | 127k | 69k | 1.9k | -99% | 1.5k | 1.7k | 0.54k | -65% | 3 |
| t08 | #10120 | 102k | 56k | 1.8k | -98% | 1.6k | 1.1k | 0.50k | -69% | 3 |
| t09 | #10118 | 1,927k | 656k | 544k | -72% | 7.6k | 3.7k | 4.54k | -41% | 32 |
| t10 | #10115 | 388k | 162k | 1.9k | -100% | 3.0k | 2.5k | 0.54k | -82% | 3 |
| t11 | #10121 | 601k | 483k | 144k | -76% | 3.6k | 2.4k | 3.59k | +0% | 24 |
| t12 | #10122 | 528k | 249k | 3.0k | -99% | 2.6k | 2.7k | 0.65k | -75% | 4 |
| t13 | #10123 | 564k | 145k | 1.8k | -100% | 3.6k | 2.2k | 0.52k | -86% | 3 |
| t14 | #10124 | 73k | 103k | 1.8k | -98% | 1.7k | 1.6k | 0.51k | -70% | 3 |
| t15 | #10125 | 775k | 330k | 2.9k | -100% | 3.5k | 2.6k | 0.63k | -82% | 4 |
| t16 (r1 FAIL) | #10126 | 1,236k | 2,135k | 13k | -99% | 5.0k | 8.5k | 0.82k | -84% | 7 |
| t17 | #10127 | 1,270k | 156k | 5.8k | -100% | 5.7k | 1.9k | 0.56k | -90% | 4 |
| t18 | #10128 | 65k | 35k | 2.0k | -97% | 1.5k | 1.2k | 0.55k | -64% | 3 |
| t19 | #10129 | 88k | 14k | 2.9k | -97% | 2.1k | 0.8k | 0.62k | -70% | 4 |
| t08 first try (PUBLISH_FAIL, push rejected) | none | | | 0.3k | | | | 0.41k | | 1 |
| **total** (r3 incl. failed t08 try) | | **17.80M** | **7.44M** | **824k** | **-95%** | **81.9k** | **51.0k** | **20.3k** | **-75%** | |

**Rep 3 vs rep 2:** input -89%, output -60%.
**16 one-shot types:** 3-7 calls each, ~1.8-13k input. The three outliers (t02, t09, t11) are 94% of rep-3 input; the other 16 types total 50k.

## Outliers (all three: the template was green, CI was not)
- **t09 (544k):** the first CI run was RED on acceptance tests unrelated to the change (an MCP acceptance flake after SQL Server handshake warnings). The v7 flake detector only reruns cancelled/timed-out/no-report jobs. Rerunning Invoke pushed nothing (same tree), so the agent edited acceptance tests to force a new commit. That meant 13 reads and 3 Invoke calls.
- **t11 (144k):** same pattern. A RED acceptance test (a visibility assert) outside the template; the agent reverted, found "same HEAD", then edited the test to force a commit.
- **t02 (86k):** a real infrastructure flake (Integration Build ARM SQLite). Wait reran it, but on the next poll the *old* failed check-run still showed as completed, so Wait reported RED. The state file then blocked another rerun, and the agent deleted it by hand. CI attempt 2 was green.

## v8 fixes (rep 4)
- **Wait-PrChecks:** failed Acceptance/Integration jobs are rerun once automatically. A real failure reproduces on the rerun and is then reported.
- **Wait-PrChecks:** 120 s grace after a rerun, so stale failed check-runs aren't reported as RED.
- **Wait-PrChecks:** `-ForceRerun` switch for one manual rerun.
- **Publish:** prints `PUBLISH=NO_CHANGE` when the tree is unchanged.
- **Skill:** never edit unrelated tests to force a commit; use `-ForceRerun`.
- **Templates:** unchanged. They were green; the rep-3 test-hardening edits are not folded in.
