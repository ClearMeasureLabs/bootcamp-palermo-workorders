---
name: /feature-loop-dispatch
id: feature-loop-dispatch
category: Workflow
description: "Orchestrate a batch of work items: children-first tree, one Task per item, epic clamp, stall watchdog"
---

Run **feature-loop dispatch** as the orchestrator for a batch of authorized work items.

**Input:** Space-separated GitHub issue numbers after `/feature-loop-dispatch` (e.g. `/feature-loop-dispatch 100 101 102`). If none are given, ask once for the list.

1. Read and follow `.cursor/skills/feature-loop-dispatch/SKILL.md` completely.
2. Also follow `.cursor/skills/feature-loop/SKILL.md` for per-item rules and `.claude/factory-loop.json` for board/build config.
3. Start the stall watchdog (`Check-StalledLanes.ps1`, GitHub-visible stalls only — no Cursor `-TasksDir`), resolve the epic/child tree, dispatch **one item-coordinator Task per issue** (`best-of-n-runner`, Sonnet pin, cap 3), enforce parent board clamp, and run until every authorized item is Done or hard-blocked.

Do not edit application code in the orchestrator session — only dispatch, verify, clamp, merge/card/issue closeout, and spawn closer Tasks when a stall needs code fixes.
