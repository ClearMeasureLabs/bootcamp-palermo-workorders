# Phase 6 round 3 (v16g, de86e425): t01-t08

Measured with `scripts/agent/measure_transcript.py` + `attribute_transcript_tokens.py`, bytes/4. 8/8 green, no CI retries.

| Item | Issue | PR | Input | Output | Calls |
|---|---|---|---|---|---|
| t01 | #10286 | 10298 | 71.5k | 0.81k | 15 |
| t02 | #10287 | 10297 | 86.4k | 0.68k | 16 |
| t03 | #10288 | 10294 | 54.1k | 0.62k | 13 |
| t04 | #10289 | 10295 | 51.1k | 0.83k | 13 |
| t05 | #10290 | 10299 | 98.7k | 0.87k | 19 |
| t06 | #10291 | 10301 | 72.0k | 0.81k | 17 |
| t07 | #10292 | 10300 | 61.6k | 0.95k | 14 |
| t08 | #10293 | 10296 | 80.7k | 0.85k | 16 |
| **Total** | | | **576.1k** | **6.42k** | 123 |

Per run **72k** input.

## Comparison (input per run)
| Reference | Per run | Round 3 vs it |
|---|---|---|
| Phase 6 round 1 (v14g) | 296k | -76% |
| Phase 6 round 2 (v15g, path bug) | 378k | -81% |
| Phase 4 rep 3 (v11g) | 97k | -26% |
| Phase 5 (v12g) | 99.5k | -28% |
| Original general baseline | 392k | -82% |

First Phase 6 round to beat the Phase 4/5 references.

## Attribution (replayed bytes/4, all 8 runs)
| Source | Tokens | Share |
|---|---|---|
| Reads before first edit (Get-Context + file ranges) | 259.8k | 45% |
| Assistant messages | 106.3k | 18% |
| Skill file | 87.2k | 15% |
| get_mcp_tools | 38.7k | 6.7% |
| Edits | 28.4k | 5% |
| Issue body | 26.7k | 4.6% |
| Prompt | 17.7k | 3% |

Wait-call turns replay 120.4k (21%) across all sources.

## Checks
- SCRIPT_FAIL: none real (the 1 hit per run is the word in the skill/prompt text).
- Submit: 1 per run; waits: 2 per run; RISK=FIX: 0.
- Get-Context calls: t01 2, t02 2, t03 2, t04 1, t05 5, t06 2, t07 3, t08 2 (t05 broke the at-most-2 rule and was the most expensive run).
- get_mcp_tools: 1 call in t01-t07, 0 in t08. This looks platform-driven: it happens even though the rules forbid it.

## v17g levers
1. `Get-Context.ps1 -Issue N` prints the criteria with the boilerplate stripped, then outlines the types the issue names. That one turn replaces `gh issue view` plus the first Get-Context call, removing a full context replay per run.
2. GENERAL.md went from 2.6 KB to 2.4 KB (merged steps, shorter map).
