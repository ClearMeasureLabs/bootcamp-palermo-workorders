# Iterations 14-20 (estimates, bytes/4; per-turn system/tool overhead not counted)

**Method (same as 1-13):**
- **Input** = cumulative transcript bytes summed at each line that has tool calls, divided by 4.
- **Output** = (reasoning + text + tool-call args) bytes / 4.

| Iter | Variant | PR | Input | Output | Tool calls | First call | Result |
|---|---|---|---|---|---|---|---|
| 14 | v5 reference | none | ~0.23k | ~0.37k | 1 | - | `PUBLISH_FAIL: push … [remote rejected]`. The agent replied with that line and did not touch git. |
| 15 | command-list prompt | #10013 | ~37.3k | ~1.1k | 11 | backgrounded after 120 s | green; while waiting the agent read 3 scripts, a terminal file, grep/glob and get_mcp_tools |
| 16 | Title -Max 1200 | #10011 | ~0.64k | ~0.32k | 2 | 504 s, foreground | green |
| 17 | v5 repeat | #10012 | ~0.65k | ~0.32k | 2 | 505 s, foreground | green |
| 18 | hand edits + file map, then `-SkipApply` | #10016 | ~519k | ~3.4k | 41 | - | green (9 read, 6 grep, 2 glob, 13 edits, 9 terminal, 2 MCP list) |
| 19 | v5 repeat | #10014 | ~9.8k | ~0.65k | 6 | backgrounded after 300 s | green; extra get_mcp_tools and 3 terminal-file reads |
| 20 | v5 final repeat | #10015 | ~0.67k | ~0.33k | 2 | 513 s, foreground | green |

## Findings
- **Clean v5 runs (16, 17, 20)** are very stable: input 0.64-0.67k and output 0.32-0.33k, with 2 calls each. The v5 budget kept the first call at ~505-513 s (iter 10 was 554 s+).
- **The main source of variance is the runtime's foreground timeout, not the script.** In 15 and 19 the agent's terminal call was moved to the background after 120 s and 300 s, well short of 600 s. The agent then polled terminal files, which cost +9k to +37k input. Iter 15's command-list prompt did not stop that exploration: it was the worst v5-era run. So **iter 15 doesn't measure the command-list technique cleanly**; backgrounding dominated it.
- **Hand edits with the file map (iter 18 vs 17):** +518k input (~800x) and +3.1k output (~10x). Compared with iter 6 (hand edits, no file map: 1,183k / 4.6k), the file map saves ~664k input and ~1.2k output on the hand path. That replicates iter 5 vs 6 (~577k / 1.5k).
- **Iter 14:** the remote rejected the push. The cause isn't visible in the transcript (possibly a branch rule or a transient error). The v5 guard worked as designed: 1 call and no improvised repair (iter 13 needed 11 calls and produced a 54-file PR). It produced no PR, so it is not a green run.
- **Value generalization (16):** same cost as the Title 1000 runs.
