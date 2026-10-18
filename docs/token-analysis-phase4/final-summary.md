# Phase 4 final summary: general lean skill, no templates

Goal: the fewest tokens for ANY change in this codebase, without per-type patch templates. 8 new change kinds
(g01-g08, none from phase 3), 3 reps, improving only the general skill between reps. All 24 runs green.
Tokens are estimates: bytes/4 via `scripts/agent/measure_transcript.py` (input = context replayed at each tool-call
turn, summed; it reproduces phase 3's published inputs exactly).

| Item | Issue | Kind | Rep 1 (v9g) | Rep 2 (v10g) | Rep 3 (v11g) | Rep 1 -> 3 |
|---|---|---|---|---|---|---|
| g01 | #10206 | domain method | 64.0k | 64.9k | 58.2k | -9% |
| g02 | #10207 | search filter | 271.9k | 223.4k | 149.0k | -45% |
| g03 | #10208 | UI component | 36.4k | 41.9k | 36.7k | +1% |
| g04 | #10209 | MCP tool param | 526.9k | 198.6k | 181.3k | -66% |
| g05 | #10210 | DB index migration | 50.1k | 27.0k | 21.7k | -57% |
| g06 | #10211 | structured logging | 425.7k | 230.3k | 212.8k | -50% |
| g07 | #10212 | bug from symptom | 37.1k | 49.1k | 45.1k | +22% |
| g08 | #10213 | refactor | 87.2k | 109.7k | 73.9k | -15% |
| **Input total** | | | **1,499k** | **945k** | **779k** | **-48%** |
| Input per run | | | 187k | 118k | 97k | |
| vs phase 3 rep 2 general baseline (392k/run) | | | -52% | -70% | **-75%** | |
| Output total | | | 9.26k | 7.90k | 7.83k | -15% |

## What worked
- One script for any change (`Submit-Change.ps1`): verify, publish from a temp worktree, print the wait command.
- Wait script: auto rerun of flaky jobs, ignore a check stuck in queue, PENDING exits 0, 9-min calls (2 wait turns per CI).
- Explicit never-list (git status/branch, terminal/script reads, manual check polling, get_mcp_tools): stray calls 14 -> 0-2.
- A compact codebase map plus CI traps (CRAP formula, Qodana collection expressions, remoting) cut reading and failures.
- Capped searches and read-once rules: reads before the first edit fell in most items.

## What still costs tokens
- Small, self-contained changes (g01, g03, g05, g07) are already at 22-58k and are flat; most of that is the skill file,
  the issue, one or two files and two wait turns replayed per turn.
- Cross-layer changes (g02, g04, g06) still read 8-15 files before editing, and every later turn replays them.
- Second CI cycles from the CRAP gate when growing an existing method (fixed in v12g, unmeasured).

## Recommended skill and prompt
Skill: `.claude/skills/feature-loop-lean/GENERAL.md` (v13g: v12g + 3 traps/rules from phase 5) + `scripts/agent/Submit-Change.ps1` + `Wait-PrChecks.ps1` v11.
Launch prompt (replace N, BRANCH):

> Follow `.claude/skills/feature-loop-lean/GENERAL.md` for issue #N on branch BRANCH. Read only that skill file and the issue, then only the files the map points to. Edit, run `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue N -Branch BRANCH`, then run the printed WAIT_CMD until CHECKS=GREEN or CHECKS=RED. On failure fix exactly what the output names and rerun Submit-Change (same args). If CI failed only on tests unrelated to the change, use WAIT_CMD -ForceRerun once; never edit unrelated tests. Never run git checkout/commit/push or your own gh run/log digging. PUBLISH_FAIL -> reply with that line. No narration, subagents, MCP, board or journal. Never call get_mcp_tools. Do NOT merge. Reply with only the RESULT line: `RESULT=GREEN|RED PR=<url>`.

## Caveats
- Same 8 issues every rep, so later reps may benefit from skill wording learned on them. Three map lines (search-filter
  chain, "no logger test package", MCP shared date parsing) are true codebase facts but also match what g02, g06 and g08
  needed; results for those items are likely flattered. A fresh set of change kinds would confirm the general gain.
- The transcript does not record output of commands that exit non-zero (PENDING before v11, RED, build FAIL), so input
  figures slightly undercount those turns.
- Output is measured by a rule that reads lower than phase 3's published output; compare output only within phase 4.
- 1 run per item per rep; per-item swings of +/-20% between reps are within run-to-run noise (g03, g07).
- v12g's single CRAP-trap edit was not measured.
