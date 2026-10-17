# Token-reduction experiment (feature-loop dispatch)

Goal: cut Cursor cloud agent token usage per work item by 80% vs baseline. Each iteration uses one sibling issue built from [work-item-template.md](work-item-template.md). PRs are closed without merging. Maximum 20 iterations.

Cursor transcripts carry no token usage fields. Proxy = cumulative replayed context (sum over turns of prefix bytes / 4), plus turns and tool calls.

| Run | Item | PR | Agent | Turns | Tool calls | Est. cumulative tokens | Δ vs baseline | Notes |
|---|---|---|---|---|---|---|---|---|
| 0 | #9936 | — | bc-34ba5471 | 99 | 99 | ~3.1M | invalid | no diff (finding already fixed) |
| 1 | #9937 | #9938 | bc-3101faa3 | 90 | 90 | ~2.90M | **baseline** | stock skill, ~35 min |
| 2 | #9939 | | | | | | | lean v1 |

## Progress notes
- 2026-10-16: Run 0 analysed. It is an invalid baseline. A repeatable template was created and issue #9937 filed for baseline run 1. Board writes were not possible: the gh token lacks the project scope.
- 2026-10-16: Run 1 baseline recorded (~2.90M est.). Optimization v1 added (scripts/agent + feature-loop-lean). Iteration 2 filed.
