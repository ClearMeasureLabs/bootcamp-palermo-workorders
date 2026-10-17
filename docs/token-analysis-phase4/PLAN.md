# Phase 4: general lean skill (v9g), new change kinds, no templates

Goal: minimize tokens for ANY change in this codebase. Phase 3's -99.4% relied on per-type patch templates; rep 2
(general techniques only) got -58%. Phase 4 measures the general skill alone.

- Skill: `.claude/skills/feature-loop-lean/GENERAL.md` (codebase map, CI traps, minimal-read rules, no narration/subagents/MCP).
- Script: `scripts/agent/Submit-Change.ps1` (verify -> publish workspace src diff -> WAIT_CMD; `-VerifyOnly` = single local verify). Keeps v8/v9 publish, auto-rerun wait (v9 fix), PUBLISH=NO_CHANGE.
- Items (none overlap phase 3 t01-t19): g01 #10206 domain method, g02 #10207 query/handler filter, g03 #10208 UI component,
  g04 #10209 MCP tool param, g05 #10210 DB index migration, g06 #10211 structured logging, g07 #10212 bug from symptom, g08 #10213 refactor.
- Rep 1 done (v9g, 8/8 green, 1,499k input, see rep-1.md). Rep 2 uses v10g.
- Rep 2 done (v10g, 8/8 green, 945k input, -37% vs rep 1, see rep-2.md). Rep 3 uses v11g; launch prompt adds "Never call get_mcp_tools."
- Plan: 3 reps x 8 items. Between reps improve only the GENERAL skill/scripts from measured waste; never per-issue patches or hints.
- Measurement as phase 3: input = replayed context summed over turns, output = reasoning + text + tool args, bytes / 4 (estimates).
- Branches `cursor/token-exp-p4-gNN-rR`. PRs are closed unmerged.

## Launch prompt (replace N, NN, R)
Follow `.claude/skills/feature-loop-lean/GENERAL.md` for issue #N on branch cursor/token-exp-p4-gNN-rR. Read only that skill file and the issue, then only the files the map points to. Edit, run `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue N -Branch cursor/token-exp-p4-gNN-rR`, then run the printed WAIT_CMD until CHECKS=GREEN or CHECKS=RED. On failure fix exactly what the output names and rerun Submit-Change (same args). If CI failed only on tests unrelated to the change, use WAIT_CMD -ForceRerun once; never edit unrelated tests. Never run git checkout/commit/push or your own gh run/log digging. PUBLISH_FAIL -> reply with that line. No narration, subagents, MCP, board or journal. Never call get_mcp_tools. Do NOT merge. Reply with only the RESULT line: `RESULT=GREEN|RED PR=<url>`.
