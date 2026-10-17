# Run 1 — BASELINE (stock feature-loop skill)

- Agent bc-3101faa3-376d-578f-bba3-4e75e404027d · issue #9937 · PR #9938 (+18/-0; closed unmerged, branch deleted) · about 35 min wall clock · tool time 179 s.
- Token fields: **none in the transcript**. All token figures are **estimates** (bytes/4).

| Metric | Value |
|---|---|
| Transcript | 266,938 B, 250 lines |
| Assistant messages / tool-call turns | 159 / 90 |
| Tool calls | run_terminal_cmd 35 · read_file 24 · grep 16 · glob 7 · search_replace 4 · get_mcp_tools 3 · mcp 1 |
| Tool results / call args / thinking / text | 169 KB / 28 KB / 28 KB / 1.3 KB |
| **Est. replayed context (input + cache read)** | **~2.90M tokens** |
| Est. output (thinking + text + args) | ~14.5k tokens |
| Est. unique content | ~67k tokens |

## Where turns went (approximate)
| Phase | Calls | Notes |
|---|---|---|
| Skill/config/MCP discovery | 7 | feature-loop SKILL 8 KB, MCP listing 8.9 KB (one listing took 26.6 s) |
| Target lookup | 14 | Read the two target files, then 9 searches/globs that found nothing; read the base class, csproj and a stray test |
| Reading unrelated skills/scripts | 8 | otel, trufflehog x2, PrivateBuild.ps1, AcceptanceTests.ps1, build.ps1 x2, PR template |
| Docker bring-up + PrivateBuild + acceptance tests via tmux | ~20 | dockerd start, log reads (16 KB + 5 KB + 4.6 KB), ps listing 21 KB, CRAP script read twice, dotnet install |
| Edit + unit test | 6 | 4 edits; the first test command failed on audit/warnaserror |
| Board GraphQL walk + journal comments | 3 | |
| Push/PR/session comment | 3 | |
| CI/check polling after the PR | 6 | |

**Waste:** about 75% of calls were not the change itself. The heaviest items were the local Docker/PrivateBuild/acceptance cycle (largest results, about 15 min), reading unrelated skills, and CI polling. Type of work: a fully specified mechanical edit plus a test.
