# Phase 5: validation of the final general skill (v12g) on 8 new change kinds

Skill frozen at v12g (`analysis/token-reduction` 1c639351): GENERAL.md, Submit-Change.ps1, Wait-PrChecks.ps1 v11.
No map lines added or tailored for these items. One rep, measured with `scripts/agent/measure_transcript.py` (bytes/4)
and compared with phase 4 rep 3 (97k input/run) and the phase 3 rep 2 general baseline (392k/run).

| Item | Issue | Kind | Title |
|---|---|---|---|
| v01 | #10238 | validation rule | Validate work order number length on lookup |
| v02 | #10239 | new query + handler (cross-layer) | Add open work order count by assignee query |
| v03 | #10240 | UI page change | Singular "work order" in search results header |
| v04 | #10241 | Blazor component tweak (updates its own test) | Show title in last work order tooltip |
| v05 | #10242 | DB + mapping, integration-test-backed | Extend Employee first name to 120 characters |
| v06 | #10243 | CRAP-risk branch in an existing method (exercises the helper rule) | Add "due soon" due-date urgency |
| v07 | #10244 | config/option | Clamp API request timeout option to 600 seconds |
| v08 | #10245 | bug fix from a symptom | Fix login drop-down names with extra spaces |

Launch: model auto-smart, model_params {"optimize_for":"cost"}, starting_ref analysis/token-reduction,
branch cursor/token-exp-p5-vNN, title "token-exp p5 vNN". PRs are closed unmerged.

## Launch prompt (fill N and NN)
> Follow `.claude/skills/feature-loop-lean/GENERAL.md` for issue #N on branch cursor/token-exp-p5-vNN. Read only that skill file and the issue, then only the files the map points to. Edit, run `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue N -Branch cursor/token-exp-p5-vNN`, then run the printed WAIT_CMD until CHECKS=GREEN or CHECKS=RED. On failure fix exactly what the output names and rerun Submit-Change (same args). If CI failed only on tests unrelated to the change, use WAIT_CMD -ForceRerun once; never edit unrelated tests. Never run git checkout/commit/push or your own gh run/log digging. PUBLISH_FAIL -> reply with that line. No narration, subagents, MCP, board or journal. Never call get_mcp_tools. Do NOT merge. Reply with only the RESULT line: `RESULT=GREEN|RED PR=<url>`.
