# Phase 6: structural levers, fresh items every round
See PLAN.md and round-N.md. Estimates: bytes/4 via scripts/agent/measure_transcript.py.

| Round | Skill | Items | Green | Input total | Input/run | Output total |
|---|---|---|---|---|---|---|
| (ref) phase 4 rep 3 | v11g | g01-g08 | 8/8 | 779k | 97k | 7.83k |
| (ref) phase 5 | v12g | v01-v08 | 8/8 | 796k | 99.5k | 6.74k |
| 1 | v14g | r01-r08 | 8/8 | 2,365k | 296k | 14.4k |
| 2 | v15g (path bug) | s01-s08 | 8/8 | 3,025k | 378k | 26.4k |
| 3 | v16g | t01-t08 | 8/8 | 576k | 72k | 6.42k |
| 4 | v17g | u01-u08 | 7/8 (u07 already done) | 1,240k | 155k (88k w/o u05) | 8.51k |
| 5 | v18g | w01-w08 | 8/8 | 659k | 82.4k | 6.54k |
| 6 | v19g | x01-x08 | 8/8 | 524k | 65.5k | 5.65k |
| 7 | v20g | y01-y08 | 8/8 | 1,174k | 146.8k (51.4k w/o y04,y05) | 7.20k |
| 8 | v21g | z01-z08 | 8/8 | 2,917k | 364.6k (63.4k w/o z04) | 12.68k |
| 9 | v22g | w01-w08 (k9) | 8/8 | 1,961k | 245.1k (44.6k single-file) | 12.20k |
| 10 | v23g | v01-v08 (k10) | 8/8 | 622k | 77.7k | 8.51k |
| 11 | v24g | t01-t08 (k11) | 8/8 | 439k | 54.9k | 5.01k |
| 12 | v25g | s01-s08 (k12) | 8/8 | 434k | 54.2k (52.1k w/o s03 script bug) | 5.55k |
| 13 | v27g | r01-r08 (k13) | 8/8 | 267k | 33.3k | 4.92k |
| 14 | v28g | q01-q08 (k14) | 8/8 | 240k | 30.0k (max 42.0k) | 4.23k |
| 15 (validation) | v28g frozen | u01-u08 (-v28), unseen kinds | 8/8 | 670k | 83.7k (median 47.6k; 51.5k w/o u08) | 6.13k |
| 16 | v29g | p01-p08 (-v29), unseen kinds | 8/8 | 893k | 111.7k (median 84.0k) | 9.72k |
| 17 | v30g | q01-q08 (-v30), unseen kinds | 8/8 | 578k | 72.3k (median 42.6k; 46.4k w/o q02) | 8.99k |
| 18 | v31g | r01-r08 (-v31), unseen kinds | 8/8 | 315k | 39.4k (median 32.8k) | 5.56k |
| 19 | v32g | s01-s08 (-v32), unseen kinds | 8/8 | 2,081k | 260.1k (median 49.2k; 53.3k avg w/o s08 gRPC hand-edit) | 12.33k |
| 20 (session size) | v33g | t01-t08 x 3 arms (A 1/session, B 4/session, C 8/session) | 24/24 | A 276k / B 708k / C 1,326k | A 34.5k, B 88.5k, C 165.7k per item | A 3.88k / B 2.34k / C 1.60k |
| 21 | v34g | u01-u08 (-v34), unseen kinds, 1 item/session | 8/8 real GREEN (8/8 reported false RED) | 416k | 52.0k (median 42.1k; 43.6k avg w/o the 67.5k false-RED retries) | 1.02k |
| 22 (change-type c01-c08) | v35g | c01-c08 (-v35): UI+PW, multi-layer, symptom bug, CI yaml, quality batch, test-only, CRAP trap, vague | 7/8 real GREEN (c04 script NO_CHANGES) | 2,292k | 286.5k (median 277.9k) | 2.70k |
| 23 (change-type c09-c16) | v36g | c09-c16 (-v36): UI+PW, multi-layer, symptom bug, build ps1, refactor, test-only, trap, UI text | 8/8 GREEN | 4,167k | 520.8k (median 193.6k; 319.4k w/o c09) | 3.55k |
| 24 (change-type c17-c24) | v37g | c17-c24 (-v37): UI+PW, new MCP tool, 2 symptom bugs, docs-only, seal batch, Playwright test, vague | 7/8 GREEN (c17 SCRIPT_FAIL on LIMIT) | 2,128k | 266.1k (median 165.5k); c04 workflow probe 18.3k GREEN | - |
| 25 (change-type c25-c32) | v38g | c25-c32 (-v38): UI+PW, LLM multi-layer, symptom bug, workflow, cancel-token batch, options trap, vague, build ps1 | 8/8 GREEN | 3,400k | 425.0k (median 212.9k; 146.8k avg w/o c25/c26) | - |
