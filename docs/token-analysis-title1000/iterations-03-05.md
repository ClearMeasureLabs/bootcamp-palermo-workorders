# Iterations 3-5 (estimates, bytes/4; input = replayed context per model turn, output = thinking + text + tool args)

| Iter | Variant | Agent | PR | Input | Output | Calls | CI reds | Notes |
|---|---|---|---|---|---|---|---|---|
| 3 | v2 reference | bc-239dac44 | #9985 | **~97.4k** | ~2.65k | 14 | 1 (flake) | ~40 min |
| 4 | ablate T3 (no CI-wait script) | bc-4326ac5c | #9984 | ~20.5k | ~1.4k | 7 | 0 | |
| 5 | ablate T2 (hand edits, no canned change) | bc-256d5bc9 | #9986 | **~606k** | ~3.1k | 42 | 0 | |

## Why iter 3 ballooned (it should have been the cleanest)
1. Invoke hit the agent runtime's **~600 s terminal cap** and was backgrounded. The agent read the terminal file and listed MCP tools (4.4 KB) to find an await tool.
2. **CI flake:** `Acceptance Tests` hung about 28 min with no TRX ("No test report files were found") and failed at the 30-min timeout, so `Build result` went red. Wait-PrChecks v2 printed only `Build result`, and its log tail was empty.
3. The agent then **dug through logs**: job API calls, a log download, `strings` dumps of 13.7 KB, 10.2 KB and 2.7 KB. It concluded the failure was infra, pushed an **empty commit** to retrigger, and waited again (581 s).
- About 84k of the ~97k input came from the flake (and the replayed log dumps), not from the technique stack.

## Iter 4 (no CI-wait script)
The agent waited with the runtime's built-in sleep (not a tool call) between 4 `gh pr checks` polls of ~3-4 KB each. Cheap, because this run had no reds; the cost of T3's absence shows up mainly when CI fails.

## Iter 5 (no canned change)
Get-ItemContext printed 15.7 KB (8 files plus outlines), yet the agent still ran 2 globs, 3 greps (12 KB), `ls`, and **13 file reads** (razor 6.6 KB), then **12 edits** before Invoke `-SkipApply` (600 s cap, backgrounded, terminal reads). Every one of the ~40 turns replays the growing context: 606k input.
