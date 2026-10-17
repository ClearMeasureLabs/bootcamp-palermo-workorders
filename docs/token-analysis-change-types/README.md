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
| Type | Rep1 in/out | Rep2 | Rep3 | Rep4 | Rep5 | Rep1→best input | Rep1→best output |
|---|---|---|---|---|---|---|---|
| 01 | 1,814k / 7.2k | | | | | | |
| 02 | 700k / 3.2k | | | | | | |
| 03 | 1,142k / 6.7k | | | | | | |
| 04 | 3,811k / 9.1k | | | | | | |
| 05 | 1,432k / 7.1k | | | | | | |
| 06 | 1,158k / 5.4k | | | | | | |
| 07 | 127k / 1.5k | | | | | | |
| 08 | 102k / 1.6k | | | | | | |
| 09 | 1,927k / 7.6k | | | | | | |
| 10 | 388k / 3.0k | | | | | | |
| 11 | 601k / 3.6k | | | | | | |
| 12 | 528k / 2.6k | | | | | | |
| 13 | 564k / 3.6k | | | | | | |
| 14 | 73k / 1.7k | | | | | | |
| 15 | 775k / 3.5k | | | | | | |
| 16 | 1,236k / 5.0k (FAIL) | | | | | | |
| 17 | 1,270k / 5.7k | | | | | | |
| 18 | 65k / 1.5k | | | | | | |
| 19 | 88k / 2.1k | | | | | | |

## Technique ledger
| Technique | Rep | Δ input | Δ output | Notes |
|---|---|---|---|---|
| v6 base: lean PHASE3 skill, no-wait Invoke, ≤2-min wait calls | 1 | baseline: 18.0M total over 19 types | 89k total | the 2-min waits multiplied context replays |
| v6.1: publish from a temp worktree on origin/master (workspace never switches; t16 fix), follow-up commits by rerunning Invoke | 2 | tbd | tbd | |
| v6.1: full unit suite locally (no filter) + IT build | 2 | tbd | tbd | targets the t01/t04 CI reds |
| v6.1: wait ≤8 min per call | 2 | tbd | tbd | targets the wait-turn count |
| Per-type exact file map + CI traps in the issue (from rep-1 PRs) | 2 | tbd | tbd | targets discovery and acceptance reds |
| Canned per-type templates (`scripts/agent/templates/tNN.patch`, `Invoke-ChangeItem -Template tNN`) | 3 (planned) | tbd | tbd | the phase 2 dominant lever |
