# v1 lean path for the Title max-length work type (for iteration 2)

| Script | What it does | Verified on the box |
|---|---|---|
| `scripts/agent/Set-TitleMaxLength.ps1 -Max 1000` | Domain constant `WorkOrder.TitleMaxLength`, EF map, `[StringLength]` on the UI model, `maxlength` on the edit-screen input, validator `.MaximumLength`, BogusOverrides clamp, integration mapping tests (300/301 replaced by the constant), **new DbUp script with auto-numbering** (`033_ExtendWorkOrderTitleLengthTo1000.sql`, "from" length auto-detected), new model unit tests plus a bUnit maxlength test derived from the Room precedent. Idempotent; preserves the UTF-8 BOM. | clean master worktree: 10 files; 66/66 targeted unit tests pass; IntegrationTests builds with -warnaserror; **SQLite WorkOrderMappingTests 10/10 pass** |
| `scripts/agent/Wait-PrChecks.ps1 -Pr N` | **One blocking call**: settles 90 s, polls every 60 s up to 40 min until every check-run on the head completes, prints `HEAD/TOTAL/PENDING/FAILED`, and for failures prints the last 25 error lines of `gh run view --log-failed`. Exit 0 green, 1 red, 2 timeout. | against PR #9978: `TOTAL=17 FAILED=0 CHECKS=GREEN` |
| `scripts/agent/Invoke-TitleLengthItem.ps1 -Issue N -Branch B` | apply, unit tests, IT build, SQLite mapping tests, PR (Publish-AgentPr), Wait-PrChecks, in one call | components verified; end to end not yet |

Risk: a single terminal call that blocks ~15 min may be backgrounded by the agent runtime. The prompt tells the agent to await it rather than re-poll.
