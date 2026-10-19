# Final confirmation round (vFinal a14cfc57, base a63bdfa4, f01-f08)

All 8 agents replied `RESULT=GREEN` on run #1. The method is the same as rounds 32/33: tokens = transcript bytes/4, replayed at every tool-call turn (`/workspace/r28/anfinal.py`). Pre-edit tokens come from `measure_exploration.is_edit` (`anfinal2.py`). CI was read with `gh` (the copilot reviewer is ignored; it does not appear in check runs).

Control numbers in this doc are the **complete** ones. All 8 control transcripts were re-dumped at 10:05 CDT, and `anctrl.py` was rerun: total 19,109.9k, avg 2,388.7k, median 2,347.7k. The ≥ lower bounds in control-round.md (≥14,133k for 7 items) are superseded.

## Per item

| item | type | PR | tokens (k) | pre-edit (k) | pre-edit % | tool calls | Submits | real CI | delivered |
|---|---|---|---|---|---|---|---|---|---|
| f01 | UI search (bUnit+PW) | #10844 | 407.2 | 127.7 | 31.4% | 25 | 3 (1 ACC_TEST=FIX, 1 lost → CACHED) | 14 pass / 2 skip | yes: "<Status> (N)" via WorkOrderCountByStatusQuery; bUnit + Playwright |
| f02 | UI manage (bUnit+PW) | #10843 | 423.7 | 91.4 | 21.6% | 26 | 2 (1 ACC_TEST=FIX) | 14 / 2 | yes: "Save and add another" (New mode only, inside the non-read-only block); 4 bUnit + Playwright. Note: the button calls HandleSubmit directly (type=button) |
| f03 | multi-layer | #10837 | 322.7 | 74.7 | 23.2% | 25 | 1 | 14 / 2 | yes: StaleWorkOrdersQuery + validator 1-90 + handler (Assigned/InProgress, oldest first) + `list-stale-work-orders` days=14; IT + unit tests |
| f04 | multi-layer state command | #10842 | 347.6 | 114.9 | 33.1% | 23 | 1 (wide ACC 77 passed) | 14 / 2 | yes: InProgressToCancelledCommand "Abandon", `private const string Name`, creator-only, clears assignee/date; registered in StateCommandList, executor map, name string and MCP description; unit + list + IT via bus |
| f05 | vague | #10839 | 280.8 | 71.4 | 25.4% | 30 | 2 (1 compile: missing Shouldly using) | 14 / 2 | yes: "Age:" ("Today" / "N days") on the manage page from WorkOrder.GetAgeInDays/FormatAgeDisplay; 7 unit tests |
| f06 | symptom bug | #10840 | 366.7 | 129.7 | 35.4% | 31 | 3 (CRAP_RISK, then IT_TEST=FAIL on the existing failure-expecting test) | 14 / 2 | yes: unknown status returns "Unknown status 'x'. Valid statuses: … Cancelled"; help text lists Cancelled; tests updated/added |
| f07 | test-only | #10838 | 186.6 | 71.5 | 38.3% | 17 | 3 (1 test fail, 1 INSPECT=FIX MethodHasAsyncOverload) | 14 / 2 | yes: 5 NavMenu bUnit tests, only NavMenuTests.cs changed |
| f08 | BogusOverrides trap | #10841 | 637.8 | 122.2 | 19.2% | 36 | 2 (LENGTH_RISK fixed before CI) | 14 / 2 | yes: Department max 20, DbUp 033, EF HasMaxLength(20), MCP output, dropdown "Full Name (Department)", BogusOverrides clamp; IT + bUnit |

**Totals**
- Tokens: 2,973.2k total, **avg 371.7k, median 357.2k**.
- Pre-edit: 803.5k (27.0%), avg 100.4k, median 103.2k.
- 0 CI REDs: every branch had 2 Build runs (one on branch creation, one on the PR), both successful.
- 8/8 on the instructed branch; 8/8 exact RESULT reply.
- 17 Submits; 5 get_mcp_tools; 5 lost tool results (f05 search_replace 10:07:23, f01 Submit 10:12:06, f04 Wait 10:15:32, f03 Wait 10:15:52, f02 Wait 10:27:10 CDT), all recovered by a foreground rerun.

## Did the vFinal fixes fire?

| fix | fired? | evidence |
|---|---|---|
| Rescue -File after the read budget (c93 dead end) | **yes, 5 items** | 8 reads after NEXT (2 cap): f03 1, f04 1 (NO_FILE), f05 1, f06 2 (outline, then the range), f08 2. **0 probe edits** in any item (one null search_replace on f05 was a lost result). Vague item: 280.8k vs c93 1,513.1k |
| Inspect: RedundantNameQualifier when a `using` is added | **no catch needed** | only f01 added a `using` to an existing file (acceptance test) and it had no qualified names; CI Qodana passed on all 8. Local inspect did catch f07 (MethodHasAsyncOverload) before CI |
| ui-bunit-search custom-results example | **yes (f01, f02)** | f01 extended the shared StubBus (counts from rows); no custom IBus and no WorkOrderSearchResultRow. 3 Submits vs c89's 11; 407.2k vs 790.5k |
| state-command recipe | **yes (f04)** | 1 Submit, no INSPECT=FIX (private Name), all 4 registrations; 347.6k vs c83 672.3k / c92 596.4k |
| LENGTH_RISK | **yes, first time (f08)** | flagged the Department clamp in BogusOverrides locally, fixed before CI |

## New waste found

1. **Calls after NEXT that get rejected: 24 turns, 362.7k (12.2% of the round).** After NEXT, agents kept calling Get-Context with `-Find` or multi-file `-File`, and each call got the 667-byte refusal. Breakdown:
   - f06: 7 turns, 98.5k
   - f08: 5 turns, 105.3k
   - f04: 3 turns, 47.3k
   - f05: 4 turns, 31.7k

   The rescue sentence seems to invite more attempts. A one-line refusal that says "next refusal is final; edit" would cap it.
2. **get_mcp_tools: 5 calls, and the turns that issued them replayed 65.3k (2.2%).** 2 were turn-2 calls (f05, f07). 3 came mid-run: f02 and f03 called it right after a lost Wait result ("Rerunning WAIT_CMD…"), and f01 called it just before its first edit. This again supports removing MCP servers at launch rather than adding prompt text.
3. **The pre-edit share rose to 27.0%.** That is partly because totals fell, and partly because of the rejected post-NEXT calls above.

## Rounds compared

| round | guidance | avg (k) | median (k) | total (k) | max item (k) | pre-edit avg (k) / share | CI RED | right branch | delivered |
|---|---|---|---|---|---|---|---|---|---|
| 32 | v45g | 402.2 | 416.6 | 3,217.4 | 726.2 (c82) | 75.7 / 18.8% | 0 | 8/8 | 8/8 |
| 33 | v46g | 498.4 | 343.1 | 3,987.5 | 1,513.1 (c93) | 60.6 / 12.2% | 1 | 8/8 | 8/8 |
| 34 control | none | 2,388.7 | 2,347.7 | 19,109.9 | 5,531.7 (cx8) | 377.1 / 15.8% | 4 | 6/8 | 8/8 |
| **final** | **vFinal** | **371.7** | **357.2** | **2,973.2** | **637.8 (f08)** | 100.4 / 27.0% | **0** | 8/8 | 8/8 |

**By type** (avg k; r32 / r33 / control / final):

| type | r32 | r33 | control | final |
|---|---|---|---|---|
| UI | 632.8 | 515.1 | 1,669.4 | 415.5 |
| multi-layer | 620.4 | 409.4 | 2,981.3 | 335.2 |
| vague | 181.2 | 1,513.1 | 3,121.5 | 280.8 |
| bug | 195.6 | 88.6 | 839.6 | 366.7 |
| test-only | 40.6 | 90.3 | 315.9 | 186.6 |
| trap | 293.8 | 446.6 | 5,531.7 | 637.8 |

## Verdict

The final round confirms the recommendations:
- It has the lowest average of any phase-6 round: 371.7k, 17% below the guided r32+r33 average of 450.3k. The control's average is 6.4× the final's.
- 0 CI REDs, and the largest item fell to 637.8k (from 1,513.1k).
- Every targeted fix either fired and helped (rescue reads, search recipe, state command) or had nothing to catch (using-aware inspect).

Two corrections to the recommendations:
1. **LENGTH_RISK is not dead weight.** It fired on f08, so keep it.
2. **Cap the post-NEXT retries.** This is the largest remaining waste. Bug, test-only and trap items cost more than in r32/r33, and on the bug and trap items that is mostly these retries plus local fix cycles.
