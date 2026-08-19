---
name: feature-loop
description: >
  Run the feature loop on ONE GitHub work item in this repo: drive it across the
  ClearMeasureLabs project board (https://github.com/orgs/ClearMeasureLabs/projects/1)
  one column at a time — design, implement, verify — through code, tests, a conflict-free
  PR, API-verified green CI, bot-finding triage, merge, and board moves. Use when asked to
  "run the feature loop on work item #N", "work issue #N through the board", or when
  /feature-loop N is invoked. For a batch of items, use /feature-loop-dispatch instead.
---

# Feature Loop (single work item)

Drives exactly one work item end-to-end. Configuration (board, columns, build commands,
merge policy, cached board IDs) comes from `.claude/factory-loop.json` at the repo root.
These rules are the contract for this repository; a user's own global rules may add to but
never weaken them.

**Cursor:** use `.cursor/skills/feature-loop/SKILL.md` (Task tool / `best-of-n-runner`)
instead of this Claude Agent-oriented file when running in Cursor.

## Children first, depth-first (before touching the item)

Resolve `gh api repos/{owner}/{repo}/issues/{N}/sub_issues` BEFORE touching #N. Recurse to
the deepest open descendant and work the tree bottom-up: a child is driven to Done, then
its parent is reconsidered. A parent is never started while any of its descendants are
open. Independent siblings may be worked in parallel — each in its own worktree. Report
the resolved tree and execution order before starting.

## Column progression

- **One column at a time.** The item advances exactly one board column per transition —
  never skips — and only after that column's work is verified. Columns and their roles
  (design / implement / verify / terminal) are in `factory-loop.json`.
- **Independent subagent per column.** Each column's work is done by its own dedicated
  subagent; one column's deliverable is the next column's input. No single subagent
  carries the item through multiple columns.
- Non-applicable columns are passed through with a recorded no-op justification (an issue
  comment stating why the column does not apply).
- On completion of the loop's scope, leave the card in `doneForNowColumn` from
  `factory-loop.json` unless the item is genuinely Done.

## Subagent rules

- **One git worktree per writing subagent.** Any subagent that edits files, builds, or
  runs tests gets its own git worktree (`isolation: "worktree"`). Parallel subagents never
  share a checkout. Read-only search agents may use the main checkout.
- **All subagents run on Sonnet** (`model: "sonnet"`) — no stage is downgraded to Haiku.

## Build, PR, and merge gates

- Branch naming: `{username}/{branch-description}` (the account that initiated the session).
- **Private build before commit:** `pwsh -NoProfile ./PrivateBuild.ps1` must pass.
- **Acceptance tests before PR:** `pwsh -NoProfile ./AcceptanceTests.ps1`.
- **Merge master before PR (mandatory):** before pushing or opening/updating a PR, fetch
  and merge `origin/master` into the branch, resolve conflicts, and re-run the private
  build. PRs must arrive conflict-free against current master.
- **CI is API-verified only:** after pushing, poll
  `gh api repos/{owner}/{repo}/commits/{sha}/check-runs` and confirm EVERY job's
  `conclusion` is `success`. Never report a PR complete on "PR created" or a local build,
  and never infer status from a shell exit code. Fix and re-push until green.
- **Triage every bot review finding before merge:** list all review comments from
  static-analysis bots (`github-code-quality[bot]`, `github-advanced-security[bot]`,
  etc.). Every finding is either fixed in the same PR or explicitly declined with a
  one-line reply on the PR. Never merge past bot findings silently.
- Merge method and issue-close policy come from `factory-loop.json`.

## Testing policy (definition of done, non-negotiable)

No functionality is added or changed without automated tests **in the same PR** at every
applicable layer:

1. **Unit tests** (`src/UnitTests` — NUnit 4.x + Shouldly, bUnit for components, AutoBogus
   for data; test doubles prefixed `Stub`).
2. **Integration tests** (`src/IntegrationTests`) wherever the change crosses a module
   boundary (data store, MediatR handlers, HTTP endpoints, cross-project calls).
3. **Full-system tests** (`src/AcceptanceTests` — Playwright, driving the real UI with
   third-party interfaces stubbed).

If a layer genuinely does not apply, state explicitly in the PR description why. UI
features MUST have a Playwright test that actually drives that UI. Respect the Onion
Architecture dependency rules (see repo `CLAUDE.md`) — violations are auto-rejected.

## Discovered work becomes a child

When working #N spawns new work — a follow-up from testing, a defect found while
implementing, a deferred bot finding — create it as a **child sub-issue** of #N, never a
free-floating sibling, and place it in the leftmost board column:

```
child_id=$(gh api repos/{owner}/{repo}/issues/{child} --jq .id)
gh api repos/{owner}/{repo}/issues/{N}/sub_issues -X POST -F sub_issue_id=$child_id
gh api repos/{owner}/{repo}/issues/{N}/sub_issues   # verify
```

Note: the POST takes the child's numeric `id`, NOT its issue number.

## Parent clamp

A parent's board status may never be further right than the LEAST-advanced of its open
children. Before every parent card move, list the parent's sub-issues and clamp:
`parent_status = min(intended_status, min(status of each open child))`. A parent cannot
reach Done while any child is open; filing a follow-up child from a late column pulls the
parent BACK to the child's column (reopening it if closed). Record each clamp as a comment
on the parent naming the child that caused it. The parent advances again only as its
children advance.

## GitHub API budget

The GraphQL budget is shared across all agents. Use REST (`gh issue`, `gh pr`,
`gh api repos/...`) for reads and comments. Reserve GraphQL for board field mutations,
using the pre-cached IDs in `factory-loop.json` `boardIds` — re-fetch them only if a
mutation fails with an unknown-ID error. Check `gh api rate_limit` before any fan-out.

Card moves use:

```
gh api graphql -f query='mutation{updateProjectV2ItemFieldValue(input:{projectId:"<projectId>",itemId:"<itemId>",fieldId:"<statusFieldId>",value:{singleSelectOptionId:"<optionId>"}}){projectV2Item{id}}}'
```

(Find `itemId` for an issue via the org project: it is the ProjectV2 item ID, not the
issue node ID.)

## Reporting

Every status update states, in order: what happened, what it means for the work item, and
what happens next — plain software-team vocabulary (work item, defect, pull request,
build, automated tests, board status), no orchestration jargon. Finish with: final board
column, PR number, merge commit SHA, and any children created.
