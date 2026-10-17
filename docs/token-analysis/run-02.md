# Run 2 — lean v1

- Agent bc-aea9f32c-ecf5-5a8a-a258-cb318e9fb943 · issue #9939 · PR #9940 (+13/-0; closed unmerged, branch deleted) · about 4 min wall clock.
- Transcript 26,695 B, 43 lines. No token fields, so figures are **estimates**.

| Metric | Run 2 | Baseline (run 1) |
|---|---|---|
| Assistant messages / tool turns | 29 / 13 | 159 / 90 |
| Tool calls | run_terminal_cmd 5 · read_file 3 · search_replace 3 · get_mcp_tools 1 · mcp 1 | 90 |
| Est. replayed context | **~47k** | ~2.90M |
| Est. output | ~2.5k | ~14.5k |
| **Reduction (replayed context)** | **-98.4%** | |

## Remaining waste (7 of 13 calls)
- Re-read SRC and TEST even though Get-AgentTarget had already printed them (2 reads).
- Looked up its agent URL: get_mcp_tools took **31.8 s**, plus an mcp call and env probing.
- Ran a separate `gh issue view` to get the title.
- A stray third edit after the PR.
- Read the skill file (unavoidable unless the prompt inlines it).
