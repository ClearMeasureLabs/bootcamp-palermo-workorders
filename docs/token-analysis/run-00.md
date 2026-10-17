# Run 0 — INVALID baseline (no diff)

- Agent: bc-34ba5471-2ca8-53c8-95c6-da49b2d7f2c7. Item: child #9936 of #9436 (Qodana redundant interpolation in WorkOrderChatHandler). The finding was already fixed on master, so there was no PR. #9936 was closed as not planned.
- Wall clock: ~10.0 min (first to last tool timestamp); total tool time 86.6 s.
- **Token fields: none in the transcript** (keys: role, text, thinking, tool_calls, tool_name, tool_args, tool_result, timing). The numbers below are proxies.

| Metric | Value |
|---|---|
| Transcript size | 277,313 B, 249 lines |
| Messages | 149 assistant (46 thinking, 5 text, 99 tool-call turns), 99 tool results, 1 user |
| Bytes by kind | tool results 145 KB · tool-call args 36 KB · thinking 48 KB · text 3.5 KB |
| Unique-content tokens (bytes/4), **estimate** | ~69k |
| Cumulative context replayed across 99 turns (sum of prefix bytes/4), **estimate of input+cache-read** | ~3.1M |
| Output (thinking + text + args)/4, **estimate** | ~22k |

## Tool calls (99)
grep 35 · read_file 28 · run_terminal_cmd 26 · glob 6 · get_mcp_tools 3 · mcp 1

## Waste
1. **Target discovery was about 70% of the run**: SARIF parsing, more than 30 greps that returned nothing, and reads of more than 15 candidate files. It then spent git archaeology working out that the finding was already fixed. The fix: give the agent the exact file and change.
2. **Skill/config reading**: feature-loop SKILL.md (8 KB), factory-loop.json, a missing .cursor skill, and a commands file. MCP tool listing alone returned 8.8 KB.
3. **Looking up session-comment conventions** by searching older issues (#9475, #9845). The fix: a canned comment script.
4. **Build trial and error**: a first `-warnaserror` build failed on restore. It then read global.json, build.ps1 and NuGet.Config before rebuilding, and that rebuild returned 17.6 KB of output. The fix: one canned build+test command with quiet output.
5. **Board walk**: several GraphQL mutations plus journal comments. That is about 8 calls and 20 s.

Type of work: discovery-heavy mechanical fix. Most tokens come from replaying context over many turns, so **turn count** is the main lever.
