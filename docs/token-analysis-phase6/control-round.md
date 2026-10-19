# Control round: no agent instructions (cx1-cx8)

Runs after round 33. Does not touch round 33 (`cursor/token-exp-p6-c89..c96-v46`, issues #10797-10804).

## Base

`cursor/token-exp-p6-ctrl-base` = master 652c36c0 plus one commit, c3cfbbed3e1188f6e7994abe08fb91e1e013a352. That commit deletes 176 files and edits 1. App code, tests, build scripts, CI workflows, `.github/scripts`, `.githooks`, `.vscode`, labs and docs are unchanged.

Removed:
- `CLAUDE.md`, `AGENTS.md`, `.cursorrules`
- `.claude/` (all of it: commands/opsx, `factory-loop.json`, every skill including feature-loop and feature-loop-dispatch)
- `.cursor/rules/`, `.cursor/skills/`, `.cursor/commands/`
- `.github/copilot-instructions.md`, `.github/copilot-code-review-instructions.md`, `.github/prompts/`, `.github/skills/`, `.github/SQUAD_BOARD.md`
- `.bob/` (ai-factory-executor skill), `.squad/` (agent squad logs/decisions), `openspec/` (agent spec changes)
- `docs/prompt-locations.md`, `docs/ready-to-move.md` (AI factory agent process docs)
- `src/.mcp.json` (agent MCP client config)
- the `..\.github\copilot-instructions.md` solution item line in `src/ChurchBulletin.sln`

Kept on purpose: `.cursor/environment.json` + `.cursor/setup.sh`. These install .NET 10, pwsh, and Docker for the cloud agent and contain no guidance. Removing them would test the environment, not the instructions. `scripts/agent/` and the lean loop (`.claude/skills/feature-loop-lean`) exist only on `analysis/token-reduction`. Master never had them.

Rulesets: the active ones are "Copilot review" (~ALL: deletion, non_fast_forward, copilot review), "master by PR" (default branch only), and "Require 2 build checks", which is disabled. None is pattern-specific, so `-ctrl` branches get the same rules as `-v46` branches. `build.yml` triggers on any push, `secret-scan.yml` on pull_request, and no workflow filters on branch name.

## Items

| id | issue | branch | category |
|---|---|---|---|
| cx1 | #10813 | cursor/token-exp-p6-cx1-ctrl | UI search (bUnit + Playwright): status summary line |
| cx2 | #10814 | cursor/token-exp-p6-cx2-ctrl | UI manage (bUnit + Playwright): instruction templates |
| cx3 | #10815 | cursor/token-exp-p6-cx3-ctrl | multi-layer: employee workload query + `get-employee-workload` MCP tool |
| cx4 | #10816 | cursor/token-exp-p6-cx4-ctrl | multi-layer state command: Restore (Cancelled → Draft, `CancelledToDraftCommand`) |
| cx5 | #10817 | cursor/token-exp-p6-cx5-ctrl | vague: can't tell if saving worked (save navigates to search silently) |
| cx6 | #10818 | cursor/token-exp-p6-cx6-ctrl | symptom bug: Room sort puts blank rooms first (`ApplySortByRoom` OrderBy with null) |
| cx7 | #10819 | cursor/token-exp-p6-cx7-ctrl | test-only: `ChurchTimeZone` unit tests |
| cx8 | #10820 | cursor/token-exp-p6-cx8-ctrl | trap: WorkOrder.AssetTag max 10 + DbUp 033 (needs a BogusOverrides clamp or CI truncates) |

## Launch prompt (bare)

> Implement GitHub issue #N in this repo on branch B. Open a pull request and get CI green. Do NOT merge. Reply with only RESULT=GREEN|RED PR=<url>.

## Measurement (same as phase 6, plus exploration)

- Tokens = transcript bytes/4, summed over every tool-call turn (`scripts/agent/measure_transcript.py`); per item, average, median, total.
- Real CI from `gh pr checks` (expect 14 pass + 2 skipping; ignore copilot); script RED count plus any reruns or flakes seen in the transcript.
- Correctly delivered: from the `gh pr diff`/files of each PR against its issue (not hook-only, no test deletions, cx8 clamp + DbUp, cx5 visible change, cx7 no production change).
- Calls, Submits n/a (count pushes / CI cycles instead), wall time, lost tool results, 10-min cuts.
- **Exploration before first edit**: `scripts/agent/measure_exploration.py` sums the same replayed input over turns before the first edit call (search_replace / write / edit / delete tool, or a shell command that writes a file). Reports k tokens, % of item total, and calls before the first edit.

Baseline for the exploration metric (round 32, v45g, same script): pre-edit 605.4k of 3,217.4k (18.8%). Avg 75.7k, median 59.7k. Calls before first edit: 13, 9, 16, 6, 11, 10, 4, 11 (c81-c88). Run it on round 33 too, so v46 and control are measured the same way.
