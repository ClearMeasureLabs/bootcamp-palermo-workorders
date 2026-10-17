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

## Results

Final summary: final-summary.md. Details in rep-1.md to rep-5.md.

### Totals per rep
| Rep | Scripts | Green | Input | vs rep 1 | vs previous rep | Output | vs rep 1 | vs previous rep |
|---|---|---|---|---|---|---|---|---|
| 1 | v6 (general only) | 18/19 (t16 PUBLISH_FAIL) | 17,801k | - | - | 81.9k | - | - |
| 2 | v6.1 (worktree publish, full unit suite, 8-min waits, file maps + traps) | 19/19 | 7,445k | -58% | -58% | 51.0k | -38% | -38% |
| 3 | v7/v7.1 (templates, CRAP/Qodana output, push retry) | 19/19 (+1 failed t08 try, push rejected) | 824k | -95% | -89% | 20.3k | -75% | -60% |
| 4 | v8 (flaky rerun, grace, -ForceRerun) | 19/19 | 75k | -99.6% | -91% | 11.0k | -87% | -46% |
| 5 | v8 unchanged (stability) | 19/19 | 106k | -99.4% | +42% | 12.0k | -85% | +9% |

### Per type
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

**Stability (rep 5 vs rep 4):** input +42% and output +9%, driven by one t04 flake/rerun edge case. Excluding t04, rep 5 is below rep 4. 13/19 types took the minimum 3 calls.

## Technique ledger
| Technique | Rep | Δ input | Δ output | Notes |
|---|---|---|---|---|
| v6 base: lean PHASE3 skill, no-wait Invoke, ≤2-min wait calls | 1 | baseline: 17.80M total over 19 types | 82k total | the 2-min waits multiplied context replays |
| **v6.1 bundle** (rep 2 vs rep 1, all 19 types): worktree publish, full unit suite, ≤8-min waits, exact file maps + CI traps | 2 | **17.80M → 7.44M (-58%)** | **81.9k → 51.0k (-38%)** | 19/19 green (rep 1: 18/19); waits 5-24 → 2-6 per run; the four changes shipped together, so they aren't separated |
| **v7 bundle**: canned per-type templates (from green rep-2 PRs) + CRAP/Qodana in wait output + no-digging skill (rep 3 vs rep 2) | 3 | **7.44M → 824k (-89%)** | **51.0k → 20.3k (-60%)** | 16/19 types one-shot (3-7 calls); templates dominate. Outliers were CI flakes the wait script didn't handle |
| v7.1: push retry on bare "remote rejected (failed)" | 3 | n/a | n/a | the t08 relaunch was clean (3 calls) |
| **v8**: auto-rerun flaky acceptance/integration jobs, post-rerun grace, -ForceRerun, NO_CHANGE notice (rep 4 vs rep 3) | 4 | **824k → 74.5k (-91%)** | **20.3k → 11.0k (-46%)** | 19/19 one-shot (3-7 calls); one auto-rerun (t14) absorbed with no agent action; leftover: stray get_mcp_tools (~4k input each) |
| v8 unchanged (stability rep, rep 5 vs rep 4) | 5 | 75k → 106k (+42%) | 11.0k → 12.0k (+9%) | t04 rerun edge case (58.6k) |
| v9 (post-experiment, untested in a rep): record a rerun only when `gh run rerun` succeeds | - | - | - | fixes the t04 cause |

Rep 3 issues: T01 #10092, T02 #10093, T03 #10094, T04 #10095, T05 #10096, T06 #10097, T07 #10098, T08 #10099, T09 #10100, T10 #10101, T11 #10102, T12 #10103, T13 #10104, T14 #10105, T15 #10106, T16 #10107, T17 #10108, T18 #10109, T19 #10110.

Rep 4 issues (branch `cursor/token-exp-p3-tTT-r4`): T01 #10130, T02 #10131, T03 #10132, T04 #10133, T05 #10134, T06 #10135, T07 #10136, T08 #10137, T09 #10138, T10 #10139, T11 #10140, T12 #10141, T13 #10142, T14 #10143, T15 #10144, T16 #10145, T17 #10146, T18 #10147, T19 #10148.

Rep 5 issues (branch `cursor/token-exp-p3-tTT-r5`, v8 unchanged): T01 #10168, T02 #10169, T03 #10170, T04 #10171, T05 #10172, T06 #10173, T07 #10174, T08 #10175, T09 #10176, T10 #10177, T11 #10178, T12 #10179, T13 #10180, T14 #10181, T15 #10182, T16 #10183, T17 #10184, T18 #10185, T19 #10186.
