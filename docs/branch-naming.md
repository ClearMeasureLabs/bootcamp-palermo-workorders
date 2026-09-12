# Branch Naming Convention for Automated Agents

## Canonical agent pattern

Board-driven automated agents (for example IBM Bob) use:

```
<tool>/<issue-number>-<column>
```

Example: `ibmbob/1234-development`

- `<tool>` — agent or tool name (e.g. `ibmbob`)
- `<issue-number>` — work item / issue number
- `<column>` — board column the work is in (e.g. `development`)

## Other patterns still in use

This repo still documents and uses additional branch patterns. Do not assume a single naming scheme everywhere:

| Pattern | Where it appears | Example |
|---|---|---|
| `<tool>/<issue-number>-<column>` | Canonical for board-driven agents (this page) | `ibmbob/1234-development` |
| `feature/issue-{number}-{slug}` | AI Factory executor (`.bob/skills/ai-factory-executor`) | `feature/issue-6936-demo-issue-4` |
| `{username}/{branch-description}` | General Copilot / contributor guidance (`.github/copilot-instructions.md`) | `jeffreypalermo/fix-work-order-status` |

Prefer the pattern required by the factory or instructions that launched the session. When in doubt for a board-driven IBM Bob session, use `<tool>/<issue-number>-<column>`.

## Branch lifecycle and checkout

### Current AI Factory executor behavior

The checked-in AI Factory scripts still create the working branch **inside** the agent session after clone:

- `agent-entrypoint.ps1` runs `git checkout -b $branchName` after cloning
- `executor.ps1` (direct mode) likewise runs `git checkout -b $branchName`

Those paths use the `feature/issue-{number}-{slug}` name from the executor, not the canonical `<tool>/…` pattern above.

### When a remote branch already exists

Some workflows publish the branch to the remote **before** the agent starts. In that case:

1. **Do not** create a new local branch with `git checkout -b <branch>`.
2. **Do** check out the existing branch (for example `git fetch origin <branch>` then `git checkout <branch>`, or `git switch <branch>` / `git checkout --track origin/<branch>` as appropriate).
3. Push commits to that same branch.

`git checkout -b <branch>` on a fresh clone does **not** reliably fail just because the branch exists on the remote. It can create a **new** local branch from the current HEAD (often `master`), which is a different history than `origin/<branch>`. Always check out / track the existing remote branch instead of using `-b` when the factory already published one.
