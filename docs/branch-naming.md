# Branch Naming Convention for Automated Agents

Automated agents use the following pattern when working on issues:

```
<tool>/<issue-number>-<column>
```

For example: `ibmbob/1234-development`

## Key Rules

1. **Pattern** — branches follow `<tool>/<issue-number>-<column>`, where `<tool>` is the agent name, `<issue-number>` is the work item number, and `<column>` is the board column (e.g. `development`).
2. **Pre-created by the factory** — the branch is created by the AI Factory **before** the agent session starts. The agent must not create it again.
3. **Check out and push** — agents must check out the pre-existing branch and push commits to it; running `git checkout -b` will fail because the branch already exists.
