# Round 31: v44g (eb0a6753), c73-c80 (#10765-10772), 1 item per session

The launch prompt now also says: "Your first action is reading GENERAL.md; do not call any MCP tool (never get_mcp_tools)."

**Method** (same as round 30):
- **Tokens:** `measure_transcript.py`, i.e. bytes/4 over the full input replayed at every turn that records a tool call.
- **Real CI:** `gh pr checks`, ignoring the copilot reviewer.
- **Durations:** `duration_ms` in the transcripts.
- **Delivery:** every PR diff was reviewed against its issue.

| id | category | PR | tokens | calls | Submits | CI REDs | wait calls | wall | real CI | correctly delivered |
|---|---|---|---|---|---|---|---|---|---|---|
| c73 | UI ("(you)" in search rows) | 10780 | 2,585.7k | 74 | 17 | 1 | 5 | 94 min | GREEN (14 pass, 2 skipping) | yes: `my-work-order` class plus "(you)" for the current user; bUnit tests; Playwright test that seeds its data through the DB |
| c74 | UI (Assign to me) | 10775 | 300.6k | 23 | 3 | 0 | 2 | 19 min | GREEN | yes: button only when the select is enabled (inside the not-read-only block), no command run; bUnit + Playwright |
| c75 | multi-layer (title filter) | 10779 | 701.0k | 32 | 4 | 0 | 1 | 21 min | GREEN | yes: `MatchTitle`/`TitleContains` with ToLower+Contains, search box, Clear filters empties it, MCP `titleContains`, integration + unit tests |
| c76 | multi-layer (open work orders in the same room) | 10776 | 541.6k | 32 | 2 | 0 | 1 | 18 min | GREEN | yes: query, validator, handler (Draft/Assigned/InProgress, excludes the current number), edit-mode section with links, integration + validator + bUnit tests; the new query was added to StubBus and to WorkOrderManageSubmitTests' own stub bus |
| c77 | vague (statuses unexplained) | 10774 | 56.3k | 13 | 1 | 0 | 2 | 17 min | GREEN | **no**: added `Core/Model/WorkOrderStatusGuide` and its tests, but nothing shows it; no page, tooltip or API uses it, so users see no change |
| c78 | symptom bug (blank assignee) | 10778 | 430.0k | 30 | 8 | 0 | 2 | 21 min | GREEN | yes: `ResolveAssigneeForSaveAsync` returns null for ""/null; bUnit test |
| c79 | test-only | 10773 | 103.1k | 15 | 1 | 0 | 2 | 16 min | GREEN | yes: DiagnosticController and WorkOrderSearchSpecification tests; no production change |
| c80 | trap (VendorTicketNumber) | 10777 | 641.5k | 39 | 5 | 0 | 2 | 20 min | GREEN | yes: **DbUp `033_AddVendorTicketNumberToWorkOrder.sql`** (NVARCHAR(20) NULL), `HasMaxLength(20)`, **`order.VendorTicketNumber.ClampLength(0, 20)` in the `case WorkOrder` block of BogusOverrides**, form field with maxlength, integration + bUnit tests |

| summary | value |
|---|---|
| Total | 5,359.8k |
| Average | 670.0k (without c73: 396.3k) |
| Median | 485.8k (without c73: 430.0k) |
| Round 30 | average 684.4k, median 588.3k |
| Real GREEN | 8/8 |
| Correctly delivered | 7/8 (c77 has no user-visible change) |
| Seeding leaks | none (no PR touches `ServerFixture.cs`) |
| Large test deletions | none (largest deletion -5 lines) |

## v44 changes
- **ui-bunit-manage recipe:** on the two manage-page items, c74 needed **3 Submits** (1 QODANA_RISK, 1 own-test failure) and c76 needed **2** (1 own-test failure). There was no setup or compile churn. In round 30, the manage-page items took 17 (c65) and 6 (c67) Submits; c65's were mostly bUnit setup compile errors.
- **On-load query trap line:** c76 put the new query in StubBus and in WorkOrderManageSubmitTests' own stub bus on the first try. In round 30, c67 edited 12 test files for the same kind of change.
- **Wide state-command acceptance:** not exercised. No item touched `Core/Model/StateCommands` or `StateCommandList`.
- **8-min budget:** the longest call was 7.6 min, and **0 calls hit the 10-min cut**. But see "Lost tool results" below.

## Flags and MCP
- **LENGTH_RISK on c80: 0 firings, and correctly so.** c80 read BogusOverrides through Get-Context and added the ClampLength line in the `case WorkOrder` block before its first Submit, following the GENERAL trap line. Its CI went green on the first cycle. This round did not test whether the rule catches a missing clamp.
- **NO_CHANGES and TEST_DELETE_RISK:** 0 firings. The only occurrences are inside GENERAL.md's own text.
- **get_mcp_tools: 7 calls** (round 30: 9).
  - c78 made none.
  - c74, c76 and c80 still called it as their **first action** (line 6), despite the launch prompt.
  - The other 4 came later: c73 looking for an AwaitShell tool after a lost result, c75 mid-edit, c77 just before its final reply, and c79 mid-task.
  - The launch-prompt line reduced the calls but did not stop them.

## Lost tool results (new failure mode)
- **7 long calls came back with no tool result** after 4.2-6.3 min. Each agent said the call "timed out".
  - **Which calls:** Wait-PrChecks on c77 (4.2 min), c79 (4.7), c80 (5.5), c78 (5.4) and c73 (5.1). Submit on c75 (6.2) and c73 (6.3).
  - **Timing:** 5 of them, on 5 different agents, ended inside one 5-minute window, **05:53-05:59 CDT**. c73's two ended at 06:14 and 06:27 CDT.
  - **Cause:** the durations differ and are below the 8-min budget, so this looks like a harness or infrastructure event, not a duration cap. The transcripts do not show the cause.
- **Cost:**
  - Each Wait was rerun once in the foreground and returned CHECKS=GREEN.
  - c75 reran Submit once.
  - **c73** switched to background runs plus `read_file` on its terminal files (10 terminal-file reads). It also made 4 direct src reads and 2 `rg`/`find` shell searches, all of which GENERAL.md forbids.
- **Unrecorded waiting:** after the switch to background runs, c73 spent **about 42 min between recorded tool calls** (7 gaps of 2-8 min, between minutes 47 and 94). The transcript does not record what it ran during those gaps.

## CI REDs and local acceptance
- **CI REDs: 1** (c73). Round 30 had 1 and round 28 had 5.
  - The failure was `FAILED_TESTS (1): ShouldShowYouMarker_InSearchRow_WhenWorkOrderAssignedToCurrentUser` in Acceptance Tests (ARM SQLite), a 60 s timeout.
  - That test had already failed the same way locally, but Submit's 7th run passed it locally before CI failed it.
- **Wait calls:** 17 over 8 CI cycles, 2.1 per cycle (round 30: 1.44). The 5 lost Wait results inflate this. The turns that issued them cost 470.9k.
- **Local acceptance results:**

| item | results |
|---|---|
| c73 | ACC_BUILD=FAIL ×3, ACC_TEST=FIX ×6, OK ×1 |
| c74, c76, c78, c80 | OK |
| c75 | OK ×2 |
| c77, c79 | SKIPPED |

## Why c73 took 94 min (2,585.7k)
1. **Reading (0-2 min):** used the full Get-Context budget (10 calls).
2. **Build failures (2-6 min):** 3 acceptance build failures, from wrong `Elements` names in its new Playwright test.
3. **Timeout loop (6-21 min):** 5 local ACC_TEST=FIX in a row, the same test each time: 60 s timeouts, then "(you)" not found. Each Submit ran about 2-3 min.
4. **First CI cycle (21-37 min):** Submit passed locally, then **CI RED** with the same test timing out in ARM SQLite.
5. **Second local loop (37-47 min):** another local FIX, then a **lost Submit result**.
6. **Background mode (47-94 min):** runs in the background with terminal-file polling, and about 42 min with no recorded calls.
   - Around minute 77 it read existing acceptance tests directly (`WorkOrderSaveThenAssignPersistenceTests`, `WorkOrderSearchTests`), which GENERAL.md forbids.
   - It rewrote the test to seed its data with `TestHost.NewDbContext` and match rows by title.
   - That version passed locally at 78.5 min and in CI at 93.7 min.

**Root causes:**
- The `ui-playwright` recipe (the CopyrightFooter test) shows no work-order data setup.
- The lost results then pushed the agent into the expensive background-polling mode.

## Proposed v45 (not built)
1. **Make reruns after a lost result cheap.**
   - Submit writes a stage stamp (HEAD tree hash plus the local checks that passed) under `/tmp/agent-submit-<issue>`. A rerun on an unchanged tree skips straight to publish and wait, taking seconds instead of about 6 min.
   - Wait-PrChecks already reruns cheaply.
   - Add one GENERAL line: "A call that returns no output: rerun the same command in the foreground; never background it or read terminal files."
   - This targets c73's 47-94 min background mode and c75's 6-min rerun.
2. **Data-setup Playwright recipe.** When the item touches the search or manage pages, `ui-playwright` adds a real exemplar that seeds a work order with an assignee via `TestHost.NewDbContext` (or `CreateAndSaveNewWorkOrder`) and asserts on a search row. This targets c73's 9 local acceptance failures and 1 CI RED.
3. **Unused new type check** in Test-ChangeRisk: a new public type under `src/Core` or `src/UI*` that no non-test file references gets `UNUSED_RISK` ("nothing uses X; wire it into the page/API the issue is about").
   - Add one GENERAL clause: vague asks need a user-visible change.
   - This targets c77's undelivered helper-only PR.

The get_mcp_tools first-action calls (3 of 8) come before any skill text. Nothing in the scripts can stop them; that is left to the launch prompt.
