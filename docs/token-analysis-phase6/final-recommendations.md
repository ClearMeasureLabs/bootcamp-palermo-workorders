# Final recommendations (phase 6 + no-instructions control)

Evidence comes from 16 guided items (round 32 = v45g, round 33 = v46g) and the no-instructions control (round 34, cx1–cx8). Tokens = transcript bytes/4, replayed at every tool-call turn. All control figures are lower bounds: cx4 was not dumped, and cx1/3/6/7 cover their first run only.

## 1. Headline: the guidance pays for itself

| | guided (16 items) | no instructions (7 measured) |
|---|---|---|
| tokens per item, avg / median | **450.3k / 370.2k** | **≥2,018.9k / ≥1,446.9k** (≥4.5× / ≥3.9×) |
| tokens before the first edit, avg | 68.1k | 387.4k (5.7×) |
| CI REDs | 1 | 4 |
| branch used as instructed | 16/16 | 6/8 |
| delivered the issue | 16/16 | 8/8 |

The biggest savings come from the item types where the repo has hidden rules: the length trap (14.9×), multi-layer work (≥4.6×) and vague asks (3.7×). Bugs and test-only items gain 2–3×.

**What the guidance does not buy:** correctness. Unguided agents delivered all 8 issues and ended green. Keep the guidance for cost, fewer CI cycles, and process compliance (branch, reply format). Do not expect it to raise quality.

## 2. Keep (measured value)

1. **Launch prompt shape:** "read GENERAL.md first; follow it for issue N on branch B; Submit with …". It gives 100% branch compliance and the exact RESULT reply (control: 2/8 off-branch, 4/7 first runs ended without a result).
2. **Get-Context with recipes.** It cuts pre-edit tokens 5.7×. Recipes that measurably cut Submits:
   - `ui-bunit-manage`: r31 c74/c76 down to 2–3 Submits.
   - `ui-bunit-search`: r33 c94 needed 1 Submit.
   - `db-column` + NEXT_SCRIPT: every trap item since r31 clamped and numbered DbUp correctly at the first Submit.
3. **Submit-Change local gates:**
   - unit + integration tests on SQLite;
   - affected/wide acceptance with headless Playwright;
   - local InspectCode;
   - risk flags.
   Guided CI REDs: 0 in r32 and 1 in r33, against 4 for the 8 unguided items.
4. **Inline CI wait with the 8-minute call budget** and the rule "empty or timed-out result → rerun the same command in the foreground". Every lost result in r32/r33 (9 in total) was handled that way, with 0 background runs and 0 terminal-file reads.
5. **Whole-tree CACHED rerun.** It saved the full local re-check on both lost Submits that had already finished (r32 c81, r33 c92).
6. **GENERAL.md trap list.** Keep it at ~4 KB; every line in it traces back to a CI RED it prevented.

## 3. Cut or don't extend

- **Do not add more anti-MCP text.** The control called get_mcp_tools 3–6 times per agent with no instructions at all, and guided r33 still had 5/8 calls even with two bans. Those calls were issued at the same moment as the first file read, before any text was seen. Cost: about 51k tokens over r33 (1.3%).
  - **Fix it at the environment:** launch these agents without MCP servers attached.
  - Keep one short line in the launch prompt; drop nothing else.
- **No further work on LENGTH_RISK, per-stage stamps or folder listing.** They are cheap to keep, but they fired 0 times in r32–r33. Agents clamp because of the recipe and the existing BogusOverrides pattern (the unguided cx8 clamped too), lost Submits had always already finished, and nobody passed a folder.
- **Legacy phase-3 scripts are not used by the vFinal loop. Do not ship them:**
  - `Invoke-*Item.ps1`, `Add-NullGuard.ps1`, `Set-*MaxLength.ps1`, `Get-AgentTarget.ps1`, `Get-ItemContext.ps1`, `Get-TestContext.ps1`, `Complete-AgentItem.ps1`;
  - `templates/t*.patch`;
  - `SKILL.md`, `PHASE3.md`.

## 4. Change (built in vFinal, a14cfc57)

1. **Read-budget dead end (r33 c93: 1,513k).**
   - What happened: the budget ran out before the agent had read the file it had to edit. It then "probed" with 41 trial edits (472k), and changing the shared greeting cost another 1,002k.
   - What vFinal does: after the budget, Get-Context allows up to 2 rescue `-File` reads of a file never read with `-File` (80 lines each; an outline does not use up the file). The NEXT message says so, and GENERAL.md was updated to match.
   - Earlier evidence: r22 c01 made 43 probe edits for the same reason.
2. **Inspect blind spot (r33 c89 CI RED).** Adding a `using` makes fully-qualified names on untouched lines redundant, so Qodana failed in CI.
   - What vFinal does: Invoke-LocalInspect now reports RedundantNameQualifier anywhere in a file whose diff adds a `using`.
   - Verified on c89's exact case: the old script printed INSPECT=OK, the new one prints 2 FIX lines.
3. **Search-page data setup (r33 c89: 11 Submits).**
   - What happened: the agent built `WorkOrderSearchResultRow` and its own IBus stub.
   - What vFinal does: the `ui-bunit-search` recipe now also prints the existing sort test that feeds rows through `new StubBus(rows)`, and its NOTE forbids custom buses and result-row construction.
4. **State-command recipe (r32 c83 and r33 c92: INSPECT=FIX MemberCanBePrivate, plus registration spread across 4 files).**
   - What vFinal does: a new `state-command` recipe prints AssignedToCancelledCommand, its validator, the StateCommandList registration and the MCP executor map. Its NOTE says: private `Name`, register in all 4 places, tests mirror AssignedToCancelledCommandTests.
   - It replaces the `new-query` recipe that wrongly fired for state commands, so these items now get 18.5 KB of context instead of 23.5 KB.

## 5. Ship to master for real use (not merged)

Copy from `analysis/token-reduction` into one PR:

| path on master | content |
|---|---|
| `.claude/skills/feature-loop-lean/GENERAL.md` | the loop (vFinal, ~4 KB) |
| `.claude/skills/feature-loop-lean/SKILL.md` | **replace** the v2 phase-3 text with 3 lines: name/description + "Follow GENERAL.md in this folder" |
| `scripts/agent/Get-Context.ps1`, `recipes.txt` | context finder + recipes |
| `scripts/agent/Submit-Change.ps1`, `Test-ChangeRisk.ps1`, `Invoke-AgentBuildTest.ps1`, `Invoke-LocalAcceptance.ps1`, `Invoke-LocalInspect.ps1`, `Publish-AgentPr.ps1`, `Wait-PrChecks.ps1`, `Update-GrpcGenerated.ps1`, `Start-NextItem.ps1` | the Submit pipeline |
| `scripts/agent/templates/acc-seed.patch` | temporary acceptance seed used by local acceptance |
| `docs/agent-launch-prompt.md` (new) | the launch prompt below + "launch without MCP servers" |
| `AGENTS.md` / `CLAUDE.md` / `.cursor/rules/cloud-agent-instructions.mdc` | one line each: "For a single GitHub issue, follow `.claude/skills/feature-loop-lean/GENERAL.md`." |

Keep off master: `docs/token-analysis*`, `scripts/agent/measure_*.py`, `attribute_transcript_tokens.py`, and the legacy scripts and templates listed in §3.

**Launch prompt (per issue):**

```
Your first action is reading .claude/skills/feature-loop-lean/GENERAL.md; do not call any MCP tool (never get_mcp_tools). Follow GENERAL.md for issue #N on branch B. Do only what it says. Start with Get-Context.ps1 -Issue N. Submit with Submit-Change.ps1 -Issue N -Branch B; it waits for CI itself. Only if no CHECKS=GREEN/RED line, run the WAIT_CMD it prints. If a script fails, follow the SCRIPT_FAIL rule in GENERAL.md. Do NOT merge. Reply with only the RESULT line.
```

This is the exact round-33 prompt, unchanged for the final round.

## 6. Open risks

- Lost tool results are a harness effect: 5–7 per round, clustered within a few seconds across agents. They are handled, but each one costs a rerun turn (~20–80k).
- The control comparison is a lower bound. Re-dump cx1/3/4/6/7 and rerun `/workspace/r28/anctrl.py` to finalize. The direction (≥4×) cannot reverse: re-dumping only adds runs.
