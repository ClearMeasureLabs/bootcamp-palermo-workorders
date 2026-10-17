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

Rep 1 → rep 2 (details in rep-1.md and rep-2.md):

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

## Technique ledger
| Technique | Rep | Δ input | Δ output | Notes |
|---|---|---|---|---|
| v6 base: lean PHASE3 skill, no-wait Invoke, ≤2-min wait calls | 1 | baseline: 17.80M total over 19 types | 82k total | the 2-min waits multiplied context replays |
| **v6.1 bundle** (rep 2 vs rep 1, all 19 types): worktree publish, full unit suite, ≤8-min waits, exact file maps + CI traps | 2 | **17.80M → 7.44M (-58%)** | **81.9k → 51.0k (-38%)** | 19/19 green (rep 1: 18/19); waits 5-24 → 2-6 per run; the four changes shipped together, so they aren't separated |
| v7: Wait prints CRAP lines and new Qodana SARIF findings on red; skill bans manual log digging | 3 | tbd | tbd | targets the t16/t05 drivers |
| Canned per-type templates (refreshed from the green rep-2 PRs; all 19 apply to master) | 3 | tbd | tbd | the phase 2 dominant lever |

Rep 3 issues: T01 #10092, T02 #10093, T03 #10094, T04 #10095, T05 #10096, T06 #10097, T07 #10098, T08 #10099, T09 #10100, T10 #10101, T11 #10102, T12 #10103, T13 #10104, T14 #10105, T15 #10106, T16 #10107, T17 #10108, T18 #10109, T19 #10110.
