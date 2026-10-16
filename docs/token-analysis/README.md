# Token-reduction experiment (feature-loop dispatch)

Goal: cut Cursor cloud agent token usage per work item by 80% vs baseline. Method: dispatch one similar work item per iteration through project 1, measure tokens, refine the skills (canned PowerShell scripts, prebuilt context), close each PR without merging, and repeat (max 20).

## Runs

| Run | Item | PR | Agent | Input | Cache read | Cache write | Output | Total | Δ vs baseline | Changes |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | _pending_ | | | | | | | | baseline | |

## Progress notes
- 2026-10-16: Branch and scaffold created. Baseline dispatch is pending: the executor session has no Cursor cloud-agent launch tool and no Cursor API key.
