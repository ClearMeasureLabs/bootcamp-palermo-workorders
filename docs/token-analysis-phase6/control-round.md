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

## Results (measured 2026-10-19, CI terminal at 10:01 CDT)

**Transcript coverage caveat.** I could not re-dump the transcripts: no transcript-dump tool was available to me. The numbers below come from the dumps already in `/workspace/cloud-agent-transcripts/`:
- cx4 (bc-31b3ba2c) was never dumped, so it is **missing**.
- cx1, cx3, cx6 and cx7 contain only their **first run**: one user turn, ending on "waiting for CI". Their later CI-subscription wake-ups added commits (cx1 8dc51303 at 09:45, cx3 fb71e9ea at ~09:48) that are not counted.
- Every control token figure is therefore a **lower bound**. To re-measure after a re-dump: `python3 /workspace/r28/anctrl.py` (writes `resctrl.json`).

| id | PR | head branch | compliant | tokens (k) | pre-edit (k) | calls before 1st edit | calls | pushes | local builds/tests | file reads (read/grep/glob) | get_mcp_tools + mcp | CI runs failed | delivered (vs ctrl-base) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| cx1 UI | 10824 | cursor/status-summary-search-88af | **no** | ≥1,446.9 (first run) | 354.0 | 26 | 55 | 1+ | 3 | 30 | 5 | 1 | yes: per-status summary line in workflow order, bUnit + Playwright |
| cx2 UI | 10825 | cursor/instruction-templates-24cb | **no** | 1,134.8 | 321.3 | 28 | 54 | 1 | 1 | 27 | 4 | 0 | yes: Insert template dropdown, append/no leading blank/max length, bUnit + Playwright |
| cx3 multi | 10827 | cx3-ctrl | yes | ≥2,350.8 (first run) | 586.8 | 41 | 81 | 1+ | 7 | 44 | 9 | 1 | yes: EmployeeWorkloadQuery + validator + handler + `get-employee-workload` + IT |
| cx4 state cmd | 10826 | cx4-ctrl | yes | not dumped | - | - | - | - | - | - | - | 1 | yes: CancelledToDraftCommand "Restore" (private Name), clears assignee/assigned/completed, MCP |
| cx5 vague | 10823 | cx5-ctrl | yes | 3,121.5 | 767.9 | 38 | 82 | 3 | 3 | 39 | 4 | 1 (+1 cancelled) | yes: save success flash message on search via WorkOrderSearchState (+108 lines vs base; the "+0" was the PR-vs-master view) |
| cx6 bug | 10822 | cx6-ctrl | yes | ≥407.2 (first run) | 78.3 | 13 | 28 | 1 | 1 | 11 | 6 | 0 | yes: blank rooms last both directions, tests updated |
| cx7 test | 10821 | cx7-ctrl | yes | ≥139.7 (first run) | 22.7 | 10 | 21 | 2 | 2 | 7 | 4 | 0 | yes: ChurchTimeZoneTests only |
| cx8 trap | 10828 | cx8-ctrl | yes | 5,531.7 | 580.7 | 36 | 115 | 1 | 14 | 54 | 7 | 0 | yes: AssetTag max 10, DbUp 033, ClampLength in BogusOverrides, form + MCP |

- Real CI: all 8 PRs end at 14 pass / 2 skipping.
- CI REDs (failed Build runs on agent commits): **4** (cx1, cx3, cx4, cx5).
- Delivered: **8/8** (judged on the diff against `cursor/token-exp-p6-ctrl-base`; the PR-vs-master view also shows the 176 removed guidance files, which I ignored).
- Branch-name compliance: **6/8** (cx1 and cx2 opened their own branches).
- Measured items (7): total **≥14,132.6k**, average **2,018.9k**, median **1,446.9k**.
- Pre-first-edit: 2,711.7k (19.2%), average 387.4k, median 354.0k.
- Agents tried to read `.cursor/skills/feature-loop/SKILL.md` and other skills that do not exist on the base. Those reads returned nothing, so no guidance leaked in. The attempt shows the agent's environment still lists skills from somewhere outside the branch.
- get_mcp_tools: 3–6 per agent, plus 1–3 real MCP calls, with **no** instructions at all. It is environment behavior, not something the guidance causes.

## Control vs guided rounds (32 = v45g, 33 = v46g)

| type | r32 (k) | r33 (k) | control (k) | control / guided avg |
|---|---|---|---|---|
| UI (2 each) | 539.4, 726.2 | 790.5, 239.7 | ≥1,446.9, 1,134.8 | ≥2.2× |
| multi-layer (2 each) | 672.3, 568.4 | 222.3, 596.4 | ≥2,350.8, n/a | ≥4.6× |
| vague | 181.2 | 1,513.1 | 3,121.5 | 3.7× |
| symptom bug | 195.6 | 88.6 | ≥407.2 | ≥2.9× |
| test-only | 40.6 | 90.3 | ≥139.7 | ≥2.1× |
| length trap | 293.8 | 446.6 | 5,531.7 | 14.9× |
| **all** | avg 402.2 / med 416.6 | avg 498.4 / med 343.1 | avg ≥2,018.9 / med ≥1,446.9 | **≥4.5× (avg 450.3 guided)** |

| | guided r32+r33 (16 items) | control (7 measured) |
|---|---|---|
| tokens per item avg / median | 450.3k / 370.2k | ≥2,018.9k / ≥1,446.9k |
| pre-first-edit avg | 68.1k | 387.4k (5.7×) |
| calls before first edit | 4–16 | 10–41 |
| CI REDs | 1 (r33) | 4 |
| delivered | 16/16 | 8/8 |
| branch compliance | 16/16 | 6/8 |
| exact RESULT reply | 15/16 | 3/7 ended with RESULT on the first run |
