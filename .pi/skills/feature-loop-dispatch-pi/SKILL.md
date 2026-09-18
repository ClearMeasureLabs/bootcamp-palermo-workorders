---
name: feature-loop-dispatch-pi
description: >
  Create a NEW Bootcamp board work item for login lastname+first-initial experiments and hand it to
  feature-loop-pi. Use when Jeffrey wants a fresh Pi feature-loop test run (drop old test branches first).
  Model: ollama/qwen36-35b-a3b-iq3s @ 49k. Never merge.
---

# Feature-loop dispatch (Pi) — 49k-optimized

## When
Starting a **new** Pi feature-loop experiment for login dropdown `LAST, I.` display.

## Steps (parent / dispatcher)

1. **Drop prior test artifacts** so Pi cannot rediscover them:
   - `git fetch --prune`
   - Delete local+remote branches matching `pi/login-lastname-initial-*`, `feature/login-lastname*`, discarded PR branches for these tests
   - Close open test PRs for those branches (`gh pr close N`) without merge
2. **Create issue** on `ClearMeasureLabs/bootcamp-palermo-workorders` with title like `Pi experiment: login dropdown LAST, I. (run TIMESTAMP)` and body copying acceptance from #9645 (display-only; tests; no auth change). Label/body note: **do not merge**.
3. **Add to board** project 1: `gh project item-add 1 --owner ClearMeasureLabs --url <issue-url>` (or GraphQL). Capture `itemId`.
4. Set Status to Conceptual Definition if not already (`optionId c61d01c4`).
5. **Invoke feature-loop-pi** on that issue number with `$env:PI_MODEL=ollama/qwen36-35b-a3b-iq3s`.
6. Monitor until `STATUS: COMPLETE|BLOCKED`. Record PR. **Do not merge.** Optionally close PR + delete branch after green for the next iteration.

## Tiny prompt template for Pi
```
You are the feature-loop-pi orchestrator. Model ollama/qwen36-35b-a3b-iq3s.
Issue #N. ITEM_ID=... 
Use skill feature-loop-pi. One column at a time. Subagents only for column work.
Copy-paste GraphQL only. DO NOT MERGE. Stop at green PR or BLOCKED.
```
