# Phase 6: structural levers, fresh items every round
See PLAN.md and round-N.md. Estimates: bytes/4 via scripts/agent/measure_transcript.py.

| Round | Skill | Items | Green | Input total | Input/run | Output total | Correctly delivered |
|---|---|---|---|---|---|---|---|
| (ref) phase 4 rep 3 | v11g | g01-g08 | 8/8 | 779k | 97k | 7.83k | not checked |
| (ref) phase 5 | v12g | v01-v08 | 8/8 | 796k | 99.5k | 6.74k | not checked |
| 1 | v14g | r01-r08 | 8/8 | 2,365k | 296k | 14.4k | not checked |
| 2 | v15g (path bug) | s01-s08 | 8/8 | 3,025k | 378k | 26.4k | not checked |
| 3 | v16g | t01-t08 | 8/8 | 576k | 72k | 6.42k | not checked |
| 4 | v17g | u01-u08 | 7/8 (u07 already done) | 1,240k | 155k (88k w/o u05) | 8.51k | not checked |
| 5 | v18g | w01-w08 | 8/8 | 659k | 82.4k | 6.54k | not checked |
| 6 | v19g | x01-x08 | 8/8 | 524k | 65.5k | 5.65k | not checked |
| 7 | v20g | y01-y08 | 8/8 | 1,174k | 146.8k (51.4k w/o y04,y05) | 7.20k | not checked |
| 8 | v21g | z01-z08 | 8/8 | 2,917k | 364.6k (63.4k w/o z04) | 12.68k | not checked |
| 9 | v22g | w01-w08 (k9) | 8/8 | 1,961k | 245.1k (44.6k single-file) | 12.20k | not checked |
| 10 | v23g | v01-v08 (k10) | 8/8 | 622k | 77.7k | 8.51k | not checked |
| 11 | v24g | t01-t08 (k11) | 8/8 | 439k | 54.9k | 5.01k | not checked |
| 12 | v25g | s01-s08 (k12) | 8/8 | 434k | 54.2k (52.1k w/o s03 script bug) | 5.55k | not checked |
| 13 | v27g | r01-r08 (k13) | 8/8 | 267k | 33.3k | 4.92k | not checked |
| 14 | v28g | q01-q08 (k14) | 8/8 | 240k | 30.0k (max 42.0k) | 4.23k | not checked |
| 15 (validation) | v28g frozen | u01-u08 (-v28), unseen kinds | 8/8 | 670k | 83.7k (median 47.6k; 51.5k w/o u08) | 6.13k | not checked |
| 16 | v29g | p01-p08 (-v29), unseen kinds | 8/8 | 893k | 111.7k (median 84.0k) | 9.72k | not checked |
| 17 | v30g | q01-q08 (-v30), unseen kinds | 8/8 | 578k | 72.3k (median 42.6k; 46.4k w/o q02) | 8.99k | not checked |
| 18 | v31g | r01-r08 (-v31), unseen kinds | 8/8 | 315k | 39.4k (median 32.8k) | 5.56k | not checked |
| 19 | v32g | s01-s08 (-v32), unseen kinds | 8/8 | 2,081k | 260.1k (median 49.2k; 53.3k avg w/o s08 gRPC hand-edit) | 12.33k | not checked |
| 20 (session size) | v33g | t01-t08 x 3 arms (A 1/session, B 4/session, C 8/session) | 24/24 | A 276k / B 708k / C 1,326k | A 34.5k, B 88.5k, C 165.7k per item | A 3.88k / B 2.34k / C 1.60k | not checked |
| 21 | v34g | u01-u08 (-v34), unseen kinds, 1 item/session | 8/8 real GREEN (8/8 reported false RED) | 416k | 52.0k (median 42.1k; 43.6k avg w/o the 67.5k false-RED retries) | 1.02k | not checked |
| 22 (change-type c01-c08) | v35g | c01-c08 (-v35): UI+PW, multi-layer, symptom bug, CI yaml, quality batch, test-only, CRAP trap, vague | 7/8 real GREEN (c04 script NO_CHANGES) | 2,292k | 286.5k (median 277.9k) | 2.70k | not checked |
| 23 (change-type c09-c16) | v36g | c09-c16 (-v36): UI+PW, multi-layer, symptom bug, build ps1, refactor, test-only, trap, UI text | 8/8 GREEN | 4,167k | 520.8k (median 193.6k; 319.4k w/o c09) | 3.55k | not checked |
| 24 (change-type c17-c24) | v37g | c17-c24 (-v37): UI+PW, new MCP tool, 2 symptom bugs, docs-only, seal batch, Playwright test, vague | 7/8 GREEN (c17 SCRIPT_FAIL on LIMIT) | 2,128k | 266.1k (median 165.5k); c04 workflow probe 18.3k GREEN | - | not checked |
| 25 (change-type c25-c32) | v38g | c25-c32 (-v38): UI+PW, LLM multi-layer, symptom bug, workflow, cancel-token batch, options trap, vague, build ps1 | 8/8 GREEN | 3,400k | 425.0k (median 212.9k; 146.8k avg w/o c25/c26) | - | not checked |
| 26 (change-type c33-c40) | v39g | c33-c40 (-v39): UI badge, gRPC, symptom bug, docs, test stabilization, header trap, vague, language setting | 8/8 real GREEN (c34/c36 empty final reply) | 3,375k | 421.9k (median 240.2k) | - | not checked |
| 27 (expensive types c41-c48) | v40g | c41-c48 (-v40): 3 UI+PW, 3 big multi-layer, vague, symptom bug | 8/8 GREEN | 11,943k | 1,492.9k (median 619.8k; 852.7k w/o c44) | - | not checked |
| 28 (wait-mode A/B c49-c56) | v41g | c49-c56 (-v41): 2 UI+PW, 2 multi-layer, vague, symptom bug, test-only, trap; keep-warm vs long wait | 8/8 GREEN | 13,515k | 1,689.4k (median 727.3k; 851.9k w/o c53) | - | not checked |
| 29 (c57-c64) | v42g | c57-c64 (-v42): 2 UI+PW, 2 multi-layer, vague, symptom bug, test-only, trap | 8/8 real GREEN, 0 CI RED (c61/c62 no product change: hook-only PRs; c60 deletes 403 test lines) | 6,947k | 868.4k (median 465.4k; 494.1k w/o c60) | - | 5/8 (c60 overwrote tests; c61/c62 hook-only) |
| 30 (c65-c72) | v43g | c65-c72 (-v43): 2 UI+PW, 2 multi-layer, vague, symptom bug, test-only, trap | 8/8 real GREEN, 1 CI RED (c68) | 5,475k | 684.4k (median 588.3k) | - | **8/8** |
