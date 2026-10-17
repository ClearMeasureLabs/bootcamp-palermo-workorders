# Phase 3: 19 change types × 5 reps (token reduction with an optimization loop)

**Method (same as phase 2):** bytes/4 estimates from cloud-agent transcripts.
- **Input** = conversation bytes replayed at each tool-call turn, summed. Per-turn system/tool overhead is not counted.
- **Output** = reasoning + text + tool args.

**Rep 1** uses v6 general techniques only:
- lean skill (`.claude/skills/feature-loop-lean/PHASE3.md`)
- terse RESULT-only prompt
- flake rerun + capped CI wait
- guarded REST publish (src/ only)
- `Invoke-ChangeItem.ps1`: build + unit tests + publish, then it prints WAIT_CMD and returns without waiting for CI
- `Wait-PrChecks -MaxCallMin 2` (≤2 min per call)

**Reps 2-5** apply per-type improvements found after each rep (canned scripts, file maps, prompt tweaks), recorded in the ledger.
**Agents:** model auto-smart, `optimize_for: cost`, starting_ref `analysis/token-reduction`.

## Rep 1 issues
| Type | Issue | Title |
|---|---|---|
| 01 | #10017 | Require Instructions to be at most 2000 characters on the edit screen |
| 02 | #10018 | Add optional Location Notes field to work orders end-to-end |
| 03 | #10019 | Allow the creator to cancel a Draft work order (Draft → Cancelled) |
| 04 | #10020 | Add a Priority dropdown (Low/Normal/High) to work orders |
| 05 | #10021 | Add a sortable Created Date column to the work order search grid |
| 06 | #10022 | Reject due dates in the past when saving a draft |
| 07 | #10023 | Rename the 'Room' label to 'Room / Area' in the UI |
| 08 | #10024 | Add a facilities-manager demo employee to the demo seed data |
| 09 | #10025 | Add a Reopen command (Complete → InProgress) with a button |
| 10 | #10026 | Expose work order count by status as an MCP tool |
| 11 | #10027 | Add a CSV export endpoint for work orders |
| 12 | #10028 | Publish a notification event when a work order is completed |
| 13 | #10029 | Only the creator may edit the Title after a work order leaves Draft |
| 14 | #10030 | Reject attachments larger than 10 MB or with executable extensions |
| 15 | #10031 | Add a read-only 'Overdue work orders' screen |
| 16 | #10032 | Send an outbound webhook when a work order is assigned |
| 17 | #10033 | Avoid N+1 assignee loads in work order search |
| 18 | #10034 | Fix Description truncation using the Instructions max length |
| 19 | #10035 | Upgrade FluentValidation 11.11.0 to 11.12.0 |

Rep 2 issues: T01 #10054, T02 #10055, T03 #10056, T04 #10057, T05 #10058, T06 #10059, T07 #10060, T08 #10061, T09 #10062, T10 #10063, T11 #10064, T12 #10065, T13 #10066, T14 #10067, T15 #10068, T16 #10069, T17 #10070, T18 #10071, T19 #10072.

## Results (per type, per rep)

Details in rep-1.md, rep-2.md and rep-3.md.

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

## Technique ledger
| Technique | Rep | Δ input | Δ output | Notes |
|---|---|---|---|---|
| v6 base: lean PHASE3 skill, no-wait Invoke, ≤2-min wait calls | 1 | baseline: 17.80M total over 19 types | 82k total | the 2-min waits multiplied context replays |
| **v6.1 bundle** (rep 2 vs rep 1, all 19 types): worktree publish, full unit suite, ≤8-min waits, exact file maps + CI traps | 2 | **17.80M → 7.44M (-58%)** | **81.9k → 51.0k (-38%)** | 19/19 green (rep 1: 18/19); waits 5-24 → 2-6 per run; the four changes shipped together, so they aren't separated |
| **v7 bundle**: canned per-type templates (from green rep-2 PRs) + CRAP/Qodana in wait output + no-digging skill (rep 3 vs rep 2) | 3 | **7.44M → 824k (-89%)** | **51.0k → 20.3k (-60%)** | 16/19 types one-shot (3-7 calls); templates dominate. Outliers were CI flakes the wait script didn't handle |
| v7.1: push retry on bare "remote rejected (failed)" | 3 | n/a | n/a | the t08 relaunch was clean (3 calls) |
| v8: auto-rerun flaky acceptance/integration jobs, post-rerun grace, -ForceRerun, NO_CHANGE notice | 4 | tbd | tbd | targets t02/t09/t11 |

Rep 3 issues: T01 #10092, T02 #10093, T03 #10094, T04 #10095, T05 #10096, T06 #10097, T07 #10098, T08 #10099, T09 #10100, T10 #10101, T11 #10102, T12 #10103, T13 #10104, T14 #10105, T15 #10106, T16 #10107, T17 #10108, T18 #10109, T19 #10110.

Rep 4 issues (branch `cursor/token-exp-p3-tTT-r4`): T01 #10130, T02 #10131, T03 #10132, T04 #10133, T05 #10134, T06 #10135, T07 #10136, T08 #10137, T09 #10138, T10 #10139, T11 #10140, T12 #10141, T13 #10142, T14 #10143, T15 #10144, T16 #10145, T17 #10146, T18 #10147, T19 #10148.
