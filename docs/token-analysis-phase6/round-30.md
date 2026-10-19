# Round 30 — v43g (1f62b311), c65-c72 (#10749-10756), 1 item/session

Method: same as round 29.
- **Tokens:** `measure_transcript.py` (bytes/4 over the full input replayed at every turn that records a tool call).
- **Real CI:** `gh pr checks`, ignoring the copilot reviewer.
- **Durations:** `duration_ms` in the transcripts.
- **Delivery:** every PR diff was checked against its issue via `gh api pulls/N/files` and `gh pr diff`.

| id | category | PR | input | calls | Submits | CI REDs | Wait calls | longest call | wall | real CI | correctly delivered |
|---|---|---|---|---|---|---|---|---|---|---|---|
| c65 | UI (cancel confirm) | 10764 | 1,399.3k | 56 | 17 | 0 | 1 | 8.3 min | 31 min | GREEN (14 pass, 2 skipping) | yes: dialog with Confirm/Keep, bUnit, acceptance step |
| c66 | UI (refresh + updated time) | 10763 | 1,302.8k | 63 | 5 | 0 | 2 | 8.4 min | 26 min | GREEN | yes: Refresh, Chicago "Updated at h:mm tt", bUnit, Playwright |
| c67 | multi-layer (open count in assignee list) | 10762 | 1,002.9k | 47 | 6 | 0 | 0 | 8.7 min | 24 min | GREEN | yes: query, validator, handler, "(N open)" text, tests |
| c68 | multi-layer (reopen) | 10760 | 813.4k | 42 | 5 | 1 | 4 | 8.6 min | 51 min | GREEN | yes: creator-only Complete → In Progress, clears CompletedDate, StateCommandList, MCP |
| c69 | vague (projector) | 10759 | 229.6k | 23 | 4 | 0 | 1 | 8.5 min | 17 min | GREEN | yes, minimal: larger search-page font sizes + a CSS test |
| c70 | symptom bug (last viewed link) | 10761 | 363.1k | 33 | 5 | 0 | 2 | 8.4 min | 19 min | GREEN | yes: LastWorkOrder also listens to WorkOrderSelectedEvent, bUnit test |
| c71 | test-only (telemetry handler) | 10757 | 99.7k | 14 | 1 | 0 | 2 | 8.8 min | 17 min | GREEN | yes: MeterListener + log test, no production change |
| c72 | trap (anonymous count API) | 10758 | 264.1k | 23 | 3 | 0 | 1 | 8.9 min | 17 min | GREEN | yes: controller, **`tools/open-work-order-count` added to ApiPublicPathRules.PublicLeaves**, unit + anonymous acceptance test |

- **Total:** 5,474.8k
- **Average:** 684.4k/item
- **Median:** 588.3k
- **Earlier rounds:** round 29 868.4k / 465.4k; round 28 1,689.4k / 727.3k.
- **Results:** 8/8 real GREEN, 8/8 RESULT lines, and **8/8 correctly delivered** (round 29: 5/8).

## Delivery checks (v43g fixes)
- **Publish clean:** no PR contains `ServerFixture.cs` or the seeding hook. Every PR touches only files for its own issue.
- **No large test deletions:** the largest deletion in any file is -10 lines (c65 `WorkOrderManage.razor.cs`). TEST_DELETE_RISK fired 0 times.
- **c72** added `"tools/open-work-order-count"` to `PublicLeaves` and extended `ApiKeyAuthenticationMiddlewarePublicPathTests`. The trap was avoided.
- **Flags:** NO_CHANGES fired 0 times; no agent submitted an empty change. LENGTH_RISK fired 0 times; no item in this round added a max-length column.

## Terminal calls and waits
- **No call reached 10 min.** The longest was 8.9 min (c72).
- **Cuts and terminal reads:** 0 calls ended at the 10.0-min cut, and there were 0 terminal-file reads (round 29: 9 cuts).
- **Unrecorded-wait estimate: 0.** Round 29's heuristic (assistant turns with no tool call that mention "wait", followed by another assistant turn) matches 20 turns here. But here those turns are thinking-only messages placed just before a recorded tool call; I checked c65's 3 matches. They are not hidden awaits. Round 29's 675.6k heuristic should therefore be read as an upper bound.
- **c67:** one Submit (8.7 min) came back with **no tool result**. The agent called it "timed out" and reran Submit (6.0 min → `PUBLISH=NO_CHANGE` + CHECKS=GREEN). That cost one extra Submit.
- **Waits:** 13 Wait calls over 9 CI cycles (1.44 per cycle), with 300.9k on the turns that issued them.

## CI REDs and local acceptance
- **CI REDs: 1** (c68). Round 29 had 0 and round 28 had 5.
  - c68's change was Core plus MCP only (a new state command). Local acceptance printed `ACC_TEST=SKIPPED` because no UI file changed.
  - In CI, Acceptance failed. Its PR touches no UI files, so the job was auto-rerun once (`FLAKE-RERUN … 12 min`), as designed. It then printed **`FAILED_TESTS (4): CompleteWorkOrderWorkflow, ShouldCompleteWorkOrder, ShouldHideDictateButtonsOnReadOnlyWorkOrder, ShouldShowSpeakButtonsOnReadOnlyWorkOrder`**. That was the first time the FAILED_TESTS path fired.
  - The Reopen button made completed work orders non-read-only. The agent added `WorkOrderFieldReadOnly` and went green on its second cycle.
- **Local acceptance results:**
  - ACC_TEST=OK: c65 ×2, c66, c67 ×2, c68 (after the RED), c72.
  - ACC_TEST=FIX: c66 ×1 (Playwright timeout in its new Refresh test).
  - ACC_BUILD=FAIL: c66 ×2 (duplicate `[Test]`, wrong `NavMenu.Elements` name).
  - SKIPPED: c68 ×2, c69 (CSS only), c70 (no acceptance class references LastWorkOrder), c71.
- **INSPECT=FIX** fired 3 times: c65 (ConvertToPrimaryConstructor and RedundantSuppressNullableWarningExpression in the new test), c67 (RedundantAnonymousTypePropertyName) and c68.
- **get_mcp_tools:** called 9 times, once per item at transcript line 6 (the first action after reading GENERAL), plus a second call on c65. c65 also made 1 `mcp` call at the end. The new top line in GENERAL.md did not stop the first-turn call.

## Outliers
- **c65, 1,399.3k (17 Submits):**
  - 13 Submits failed locally on bUnit setup for WorkOrderManage: a `SpeechSynthesis` constructor, `BunitServiceProvider` members, an obsolete `FormatterServices`, and an `ISet<Role>` collection expression.
  - 2 more were RISK=FIX on the new test and 1 was INSPECT=FIX, before 1 CI cycle went green.
- **c66, 1,302.8k (46 edits):** 2 acceptance build fails and 1 acceptance timeout fix on the new Playwright test.
- **c67, 1,002.9k:** the page now sends a new query on load, so the agent added a stub to **12 existing WorkOrderManage test files** (+3-5 lines each). It also hit 1 lost Submit result.

## Proposed v44 (not built)
1. **Map state-command changes to acceptance tests.** Changes under `src/Core/Model/StateCommands/` or `StateCommandList.cs` trigger the wide WorkOrders run in Invoke-LocalAcceptance. This would have caught c68's 4 acceptance failures locally, saving 1 CI cycle and a 12-min rerun.
2. **Recipe for the WorkOrderManage bUnit test setup.** When the target is `WorkOrderManage*`, Get-Context's `ui-bunit` recipe prints the real service setup from an existing `WorkOrderManage*Tests.cs`: SpeechSynthesis, translation stub, StubBus, user session. c65 spent 13 local Submits rediscovering it, and c67 touched 12 files.
   - Optionally, a GENERAL trap line: "a page that sends a new query on load needs it in StubBus."
3. **Lower Submit's 9-min total to 8 min** (inline wait = 8 − elapsed). This leaves margin under the observed result loss at 8.7 min (c67) as well as the 10-min cut.
4. **get_mcp_tools:** stop spending GENERAL.md bytes on it. The call is the harness's first-turn tool discovery (always at line 6) and happens before any skill instruction can act. Only the launch prompt or harness can remove it.
