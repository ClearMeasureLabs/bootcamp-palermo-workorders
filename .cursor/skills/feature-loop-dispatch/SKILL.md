---
name: feature-loop-dispatch
description: >
  Authorize a batch of work items for full autonomous implementation in this repo
  (ClearMeasureLabs/bootcamp-palermo-workorders). Resolves each item's epic/child tree,
  computes a children-first execution order, and dispatches one dedicated Task subagent
  per work item that runs the feature-loop skill end-to-end — code, tests, PR,
  API-verified green CI, merge, and board moves on
  https://github.com/orgs/ClearMeasureLabs/projects/1 — with epic clamp rules enforced.
  Use when asked to "dispatch the feature loop on ...", "implement these work items", or
  when /feature-loop-dispatch is invoked with a list of issue numbers. The invoking
  session stays alive as the orchestrator until every authorized item is Done.
---

# Feature-Loop Dispatch — Cursor

This session is the **orchestrator**. The user has authorized the listed work items for
full, unattended implementation. From this point the session runs to completion without
asking anything further — every decision is made autonomously under the rules below.

**Canonical contract:** Keep gates identical to `.claude/skills/feature-loop-dispatch/SKILL.md`
and the per-item rules in `.cursor/skills/feature-loop/SKILL.md`. This file is the Cursor
orchestrator mapping — when Claude gains a new gate, copy it here (except Agent → Task
wording).

**Before acting:** Read this file, `.cursor/skills/feature-loop/SKILL.md`, and
`.claude/factory-loop.json`. Those files are the contract; a user's own global rules may
add to but never weaken them.

## Phase 0 — Start the stall watchdog (before any dispatch)

Sub-sessions stall silently: they spawn a background CI poller and end their turn, and the
completion notification may route to the orchestrator instead of the stopped worker — so a
PR can sit fully green and unmerged. Detection must be EXTERNAL and MECHANICAL:

1. Baseline (read-only, REST-only; exit 2 = stalls found) — GitHub-visible stalls only:

   ```
   pwsh -NoProfile .claude/skills/feature-loop-dispatch/Check-StalledLanes.ps1 `
     -Repo ClearMeasureLabs/bootcamp-palermo-workorders
   ```

2. Run it as a HEARTBEAT, not an alarm: launch a background Shell that sleeps ~15 minutes,
   runs ONE check (same command as above), and EXITS UNCONDITIONALLY — its exit wakes the
   orchestrator every cycle regardless of findings. Re-arm at the end of every turn in
   which it fired. Prefer AwaitShell for bounded waits; NEVER wait open-ended on Task
   notifications alone.
3. **LOCAL_STALL / `-TasksDir`:** Cursor Tasks do **not** write Claude-style
   `{agentId}.output` files. Do **not** pass `-TasksDir` / `-ActiveIds` unless those paths
   are confirmed to exist for this session. Pre-PR liveness relies on Task completion
   notifications + `resume` + the 20-minute no-progress rule — not on LOCAL_STALL.
4. For every stall it reports, act immediately:
   - `GREEN_UNMERGED` → `Task` with `resume` on the owning agent id: "PR #N is green;
     triage bots, merge, move card, report." If unresumable:
     - Orchestrator **may** verify check-runs, post decline replies, merge (when
       write-enabled), close the issue, and move the card — **no application code edits**.
     - If any bot finding still needs a **code fix**, spawn a fresh closer Task
       (`best-of-n-runner`) — never edit code in the orchestrator.
   - `DIRTY` → order (or spawn) conflict resolution: merge master into the branch,
     rebuild, re-push, re-verify.
   - `CI_FAILED` / `CI_STUCK` → order a fix-and-repush or re-trigger.
   - `MERGED_ISSUE_OPEN` → close the issue with the merge-evidence comment (verify
     sub_issues first; an issue clamped open behind open children is NOT a stall).

## Communication standard (applies to every update, issue comment, and PR description)

Write like a software delivery leader briefing a stakeholder. Plain software-team
vocabulary only — work item, defect, pull request, build, automated tests, test run,
board status, dependency. Never invent orchestration jargon. Specifically:

- Don't say "evidence PR" — say "a pull request that commits the test-run results
  (logs/output) to the repository."
- Don't say "clamp / clamped" — say "the parent work item stays open and its board status
  moves back to match its least-finished open sub-item" (the rule itself is unchanged;
  only the wording is).
- Don't say "lane," "loop," "chain," or agent IDs in user-facing updates — say "the work
  on item #N."
- Don't say "blocks #N's passing evidence" — say "defect #X must be fixed before we can
  re-run the tests and show item #N working."
- Every status update states, in order: what happened, what it means for the work item,
  and what happens next. A reader who has not followed the session must understand it
  cold.

## Inputs

The argument is a list of GitHub issue numbers (the "authorized set"). If no argument is
given, ask once for the list before starting; that is the only permitted question.

Configuration comes from the repo's `.claude/factory-loop.json` (tracker, project, column
map, private build command, merge method, cached board IDs).

## Phase 1 — Resolve the tree (before any work)

1. `gh api rate_limit --jq .resources` — record the budget. Board IDs (project ID, Status
   field ID, Status option IDs) are pre-cached in `factory-loop.json` `boardIds` — use
   them for every card move this session. Re-fetch via GraphQL
   (`organization(login:"ClearMeasureLabs"){projectV2(number:1){...}}`) ONLY if a
   mutation fails with an unknown-ID error, and update `factory-loop.json` when you do.
   All other reads/writes use REST.
2. For every authorized item, recursively resolve
   `gh api repos/{o}/{r}/issues/{n}/sub_issues` to the deepest descendant. The full tree
   of every authorized item joins the work set — authorizing an epic authorizes its open
   descendants.
3. Build the execution order:
   - **Children first, depth-first.** A parent/epic enters the dispatch queue only after
     ALL of its open descendants are Done.
   - Open leaf items (no open children) are the initial dispatch wave.
   - Independent items (no shared ancestor-ordering constraint between them) run in
     parallel; explicitly ordered chains run sequentially.
4. Post the resolved tree and planned order as a comment on each authorized top-level
   item, and print it in the session before dispatching.

## Phase 2 — Dispatch one item-coordinator Task per work item

For each work item whose turn has arrived, launch **one item-coordinator Task** (not one
Task per column — the coordinator owns the end-to-end loop and spawns column workers
itself per `.cursor/skills/feature-loop/SKILL.md`):

### Environment (mandatory — match the host)

**Subagents MUST run in the same environment as the orchestrator host session.**

| Host session | Task `environment` | `subagent_type` |
|--------------|-------------------|-----------------|
| Cloud agent (private worker) | `"cloud"` | `"generalPurpose"` — **not** `best-of-n-runner` (cloud VMs cannot spawn isolated worktree runners on private workers) |
| Local Cursor agent | `"local"` (default) | `"best-of-n-runner"` preferred; `generalPurpose` if worktree isolation unavailable |

Never dispatch `environment: "local"` from a cloud host, or `environment: "cloud"` from a
local host. The coordinator still uses its own git worktree inside the host environment
(`git worktree add` from the repo checkout the host already has).

| Parameter | Value |
|-----------|--------|
| `subagent_type` | See environment table above; **exactly one** coordinator per item |
| `environment` | **Same as host** — omit only when the host is local (default) |
| `model` | `inherit` (Auto, cost-optimized). Named listed slugs only if the user asked for one. |
| `run_in_background` | `true` for independent items |
| Concurrency | Cap at 3 item-coordinator Tasks |

The orchestrator itself never edits application code (comments, merges, card moves, and
issue closes are allowed when write-enabled).

The Task prompt must instruct it to **run the feature loop on exactly that one work
item**, including verbatim:

> Run the feature loop on work item #N in ClearMeasureLabs/bootcamp-palermo-workorders.
> Follow this repo's `.cursor/skills/feature-loop/SKILL.md` and
> `.claude/factory-loop.json` exactly. You are the **item coordinator**: drive #N
> end-to-end, but for each board column spawn a dedicated column-worker Task
> (`generalPurpose` on cloud hosts, `best-of-n-runner` on local hosts; same
> `environment` as the orchestrator; `model: inherit` unless the user named another
> listed model) that does only that column's work — never skip columns; record
> no-op justifications for non-applicable columns; at most one delegation hop (column
> workers must not re-delegate). Work from origin/master in your worktree; merge
> origin/master into the branch and re-run the private build
> (`pwsh -NoProfile ./PrivateBuild.ps1`) before any push or PR; run
> `pwsh -NoProfile ./AcceptanceTests.ps1` before opening the PR; triage every bot review
> finding (fix or explicitly decline with a PR reply) before merge; a PR is complete only
> when every check-run `conclusion` is `success` via
> `gh api repos/{o}/{r}/commits/{sha}/check-runs` — never a shell exit code; follow the
> Testing Policy (unit + integration + full-system Playwright in the same PR, or an
> explicit stated reason a layer doesn't apply). Use REST for all reads/comments; use
> only the cached board IDs from `.claude/factory-loop.json` for card moves. Open/update
> PRs with ManagePullRequest when available; otherwise `gh pr` if write-enabled. Any
> discovered follow-up work becomes a CHILD sub-issue of #N (POST the child's numeric id
> to `repos/{o}/{r}/issues/N/sub_issues`), placed in the leftmost column — report every
> child you create in your final summary. When #N is merged and CI-verified green, move
> its card to the `doneForNowColumn` from factory-loop.json and report: final board
> column, PR number, merge commit SHA, and any children created.

**Anti-stall requirements — add these to every Task prompt verbatim:**

> ANTI-STALL RULES (mandatory): (1) NO DISPATCHER CHAINS — you may spawn Tasks for
> column work, but a Task you spawn must DO work, never merely re-delegate to another
> Task; at most one delegation hop below you. (2) SYNCHRONOUS FINISH — once CI is
> green, do the bot-finding triage, merge, issue close, and card move in the SAME turn;
> never end your turn between "CI green" and "merged". (3) When waiting on CI, poll with
> a bounded foreground loop or a background Shell you own, and after EVERY resumption
> re-check the PR state directly with `gh` before assuming anything. (4) If any gh or
> ManagePullRequest call is blocked by permissions, do not stop silently — report the
> exact blocked command and error in your final message so the orchestrator can act.
> (5) If your worktree becomes unusable, report it immediately rather than improvising
> outside it. (6) Every wait must have a deadline: if a Task or CI run you're waiting on
> has made no observable progress in 20 minutes, stop waiting, check state directly, and
> either take over the work yourself or report the blockage.

Track each dispatched Task's agent id so Phase 0 can `resume` the right worker on stall.

## Phase 3 — Epic clamp and promotion (orchestrator's job, after every completion)

When a Task reports completion:

1. Verify its claims independently: check-runs green on the merge commit, card in the
   reported column. Never take a subagent's word for CI.
2. **New children discovered** by the Task join the work set immediately, are
   dispatched under the same rules, and clamp their parent (below).
3. **Clamp every affected ancestor:** for each ancestor of the completed/changed item,
   set `ancestor_status = min(intended_status, min(status of each open child))`. An
   ancestor is never in a more senior (further right) column than its least-advanced open
   child, is pulled BACK (and reopened if closed) when a child appears behind it, and
   each clamp is recorded as a comment on the ancestor naming the child that caused it.
4. **Promote epics only by clamp release:** when the last open child of an epic reaches
   Done, advance the epic one column at a time — each column transition performed by its
   own dedicated column-worker Task per the column rules (a no-op justification pass is
   still a pass) — until it too is Done. An epic never advances in the same action that
   closed its child.
5. Dispatch the next queued item(s) whose prerequisites are now met.

## Phase 4 — Walk-away completion

The orchestrator loop continues until every item in the (grown) work set is Done or hard-
blocked. Do not stop because the session is long; use background Tasks and wait for their
notifications — but NEVER wait on notifications alone: keep the Phase 0 watchdog running
on its ≤15-minute cadence for the whole session, because a completed grandchild's
notification may route to the orchestrator instead of its stopped parent Task, leaving
that worker permanently asleep. When a notification arrives from a grandchild (an agent
this session did not spawn), `resume` the owning Task with the result — do not assume the
worker saw it. If a Task fails, read its output, fix the dispatch (new Task, corrected
prompt, or a filed child defect), and continue — a local build failure is diagnosed to
root cause, never dismissed as environmental.

Finish with a single summary: per item — final column, PR, merge SHA, children created
(and their outcomes), plus any item left blocked and exactly why.

## Hard rules (restated, non-negotiable)

- **Dispatch:** one **item-coordinator** Task per work item (end-to-end).
- **Inside that item:** one **column-worker** Task per board column; column workers never
  span columns or re-delegate.
- The orchestrator never edits application code (merge / card / issue / comment closeout
  only; code fixes require a closer Task).
- Every writing Task: same `environment` as host; `generalPurpose` on cloud private
  workers, `best-of-n-runner` on local hosts; `model: inherit` (Auto, cost-optimized),
  unless the user named a listed model.
- A parent never outranks its least-advanced open child on the board.
- CI is verified via the check-runs API only.
- REST-first; cached board IDs from `factory-loop.json`; check `rate_limit` before each
  dispatch wave.
- Claude Agent / `isolation: "worktree"` / `SendMessage` / `model: "sonnet"` wording from
  the `.claude` skills maps to Task / `best-of-n-runner` / `resume` / `inherit` (Auto,
  cost-optimized). Do not pin a named Claude slug.
