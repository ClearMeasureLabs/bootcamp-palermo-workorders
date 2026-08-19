---
name: /feature-loop
id: feature-loop
category: Workflow
description: "Drive ONE GitHub work item across the project board end-to-end (design → implement → verify, CI-verified merge)"
---

Run the **feature loop** on a single work item.

**Input:** The argument after `/feature-loop` is the GitHub issue number (e.g. `/feature-loop 1234`).

1. Read and follow `.cursor/skills/feature-loop/SKILL.md` completely.
2. Load board/build config from `.claude/factory-loop.json`.
3. Resolve children first (sub_issues), then drive #N one board column at a time through design → implement → verify, with private build, acceptance tests, API-verified green CI, bot-finding triage, merge, and card moves.

For a batch of issues, use `/feature-loop-dispatch` instead.
