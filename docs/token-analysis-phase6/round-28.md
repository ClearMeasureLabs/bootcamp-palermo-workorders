# Round 28 — v41g (f83d31aa), c49-c56 (#10717-10724), wait-mode A/B, 1 item/session

Method: `measure_transcript.py` (bytes/4 over the full input across turns). Real CI comes from `gh pr checks`, ignoring the copilot reviewer. Waits, gaps and durations come from each tool call's `duration_ms`/`started_at_ms` in the transcript.

Arms:
- **keep-warm** (`scripts/agent/wait-mode.txt` on the branch, 8a44054a): Submit waits inline for at most 4 min, and WAIT_CMD uses `-MaxCallMin 4`.
- **long** (no flag, f83d31aa): Submit waits inline for up to 9 min total, and WAIT_CMD uses `-MaxCallMin 9`, silent until a verdict.

| id | arm | category | PR | input | calls | Submits | CI REDs | Wait calls | tokens on Wait turns | tool calls > 5 min | wall | real CI |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| c49 | keep-warm | UI (pagination) | 10732 | 2,148.4k | 75 | 17 | 1 | 9 | 434.5k | 2 | 85 min | GREEN (14 pass, 2 skipping) |
| c51 | keep-warm | multi-layer (phone) | 10730 | 913.1k | 45 | 4 | 1 | 8 | 245.5k | 2 | 46 min | GREEN |
| c53 | keep-warm | vague | 10726 | 7,552.4k | 137 | 15 | 3 | 15 | 504.4k | 12 | 174 min | GREEN |
| c55 | keep-warm | test-only | 10725 | 50.0k | 10 | 1 | 0 | 2 | 18.5k | 1 | 16 min | GREEN |
| c50 | long | UI (remember me) | 10731 | 541.4k | 32 | 9 | 0 | 1 | 26.6k | 2 | 31 min | GREEN |
| c52 | long | multi-layer (LastUpdated) | 10728 | 1,484.1k | 71 | 7 | 0 | 1 | 47.5k | 2 | 19 min | GREEN |
| c54 | long | symptom bug (UTC times) | 10729 | 474.9k | 31 | 3 | 0 | 1 | 25.9k | 2 | 20 min | GREEN |
| c56 | long | trap (age label) | 10727 | 350.9k | 25 | 2 | 0 | 1 | 19.9k | 2 | 18 min | GREEN |

- **Total:** 13,515.3k
- **Average:** 1,689.4k/item; 851.9k without c53
- **Median:** 727.3k
- **Results:** 8/8 real GREEN, and all 8 replied with the RESULT line.
- **Column definitions:**
  - "CI REDs" counts CHECKS=RED verdicts from Wait. The word in GENERAL.md, which agents still read, is not counted.
  - "Tokens on Wait turns" is the bytes/4 replay of the turns that issue a Wait call.

## Against the same categories
| category | 40-item avg (r22-26) | round 27 | round 28 |
|---|---|---|---|
| UI with tests | 930.4k | 719.9k | 1,344.9k (c49 2,148.4k, c50 541.4k) |
| multi-layer | 692.0k | 3,156.1k (p90-size items) | 1,198.6k (c51 913.1k, c52 1,484.1k) |
| vague | 347.5k | 79.0k | 7,552.4k (c53) |
| symptom bug | 211.3k | 236.0k | 474.9k (c54) |
| test-only | 142.4k | - | 50.0k (c55) |
| trap | 212.9k | - | 350.9k (c56) |

## Why c53 (vague) took ~3 hours and 7.55M
- **The change:** the agent read "the list is overwhelming" as "default *Assigned to me* = on" in the search state. That change alters the default results of the search page that many existing acceptance flows land on: save → search, the MCP search parity check, due-date colours, and sort/filter.
- **Wall time:** 174 min in total: 92 min in Wait, 64 min in Submit, 16 min of model time.
- **Cycles:** 4 CI cycles, 3 of them RED. The first Submit printed `ACC_TEST=SKIPPED no acceptance class covers the changed files`. The only UI file changed was `Services/WorkOrderSearchState.cs`, and v41g maps only `.razor`/`.razor.cs` names to acceptance classes.
- **Flake reruns:** every acceptance RED was first auto-rerun as a flake (`FLAKE-RERUN … 16 min`, `… 12 min`). Wait-PrChecks reruns any failed Acceptance/Integration job once, so each real RED cost about 2 acceptance runs.
- **Rework:** the agent then patched 5 acceptance files plus AcceptanceTestBase, and ran 7 local `ACC_TEST=FIX` Submits at 5-8 min each.
- **Arm contamination:** it overrode the arm by hand-editing WAIT_CMD to `-MaxCallMin 15/20/25`. One call ran **19.4 min** and returned normally, so the terminal does allow calls longer than 10 min.

## Local acceptance build and tests (v41g #1, #2)
- **Ran on every agent VM:**
  - 0 `SKIPPED` because of Playwright or the browser.
  - The only skips: c55 (no UI change) and c53's first Submit (the selection gap described above).
- **Caught before CI:**
  - `ACC_BUILD=FAIL` 9 times: c49 6, c50 3.
  - `ACC_TEST=FIX` 14 times: c49 5, c50 1, c53 7, c54 1.
  - c54 and c50 each went green on their first CI cycle after a local FIX.
- **CI REDs fell from 8 (round 27) to 5.** All 5 were missed locally for one of three reasons:
  - **Data/order-dependent:** c49 `AssignedToMe_WhenUnchecked_RestoresFullList`. It passed locally on a fresh DB but failed with the full suite's rows: 25-row paging.
  - **SQL Server only:** c51 `String or binary data would be truncated … Employee.PhoneNumber`. Bogus generated phone numbers longer than 20 characters, and SQLite does not enforce nvarchar length.
  - **Selection gap:** c53 ×3, cross-class breakage from a change in a non-razor UI service.
- **Cost:** a code Submit with acceptance tests now takes 3-5 min before publishing. With the inline wait it took 6-8.6 min.

## Wait-mode A/B
| | keep-warm (c49, c51, c53, c55) | long (c50, c52, c54, c56) |
|---|---|---|
| CI cycles (published Submits) | 9 | 4 |
| Wait calls | 34 (3.8 per cycle) | 4 (1.0 per cycle) |
| tokens on Wait turns | 1,202.9k (133.7k per cycle) | 120.0k (30.0k per cycle) |
| tool calls > 5 min (idle gaps between model turns) | 17: 13 Submits, 4 Waits (c53's overridden waits) | 8: 4 Submits, 4 Waits |
| gaps > 5 min per CI cycle | 1.9 | 2.0 |

- **Long arm:** each item needed Submit (7.6-8.4 min, with its inline wait) plus exactly 1 Wait (7.6-8.4 min) to reach GREEN.
- **Keep-warm arm:** the 4-min check-ins kept the *Wait* gaps under 5 min (3.4-4.1 min each). However, **Submit itself runs 6-8.6 min**, so each cycle still had at least one gap over 5 min. Keep-warm therefore removed about 1 cold gap per cycle, at the cost of about 2.8 extra Wait turns per cycle.

### What we can and cannot conclude about caching
- **Cannot:** the transcripts have no usage, cache or cost fields, and no Cursor usage or cost data is reachable from the box. We **cannot** measure cached vs uncached input, and we cannot confirm the ~5-minute idle TTL. Bytes/4 counts every replay at full size in both arms.
- **Can:** keep-warm replays about 4.5× more Wait-turn bytes per CI cycle (133.7k vs 30.0k). Even if every keep-warm Wait turn hit the cache and every long-arm Wait turn missed, keep-warm only wins when cached input costs less than about 22% of uncached input (30.0/133.7). That holds before counting cache-write surcharges and the cold Submit gap that both arms share.
- **Confounds:**
  - The arms differ by item: keep-warm got the 2 most expensive items, c49 and c53.
  - c53 broke its arm by overriding `-MaxCallMin`.
  - Arm totals (10,664k vs 2,851k) are driven by item difficulty, not by waits, so they show nothing about waiting.
- **To settle it:** we need Cursor's usage export (cache-read vs input tokens per request) for these 8 bc-ids.

## Next steps (ranked)
1. **One long silent wait, and no automatic acceptance rerun.**
   - Use the long arm with `-MaxCallMin 20`. c53 showed a 19.4-min call works.
   - Stop auto-rerunning a failed Acceptance job when the PR touches UI or AcceptanceTests files, and report RED directly. Each real acceptance RED in round 28 cost an extra 12-16 min rerun.
2. **Widen local acceptance selection:**
   - Map any changed `src/UI.Shared/**.cs` type to the razor pages that use it, then to acceptance classes.
   - When `AcceptanceTestBase` or a search/manage default changes, run all of `src/AcceptanceTests/WorkOrders` and `McpServer` (c53).
   - Seed extra work orders before UI runs so paging and row-count bugs appear locally (c49).
3. **Risk rule for SQL Server-only length limits:** a new `string` property with `HasMaxLength(n)` needs a BogusOverrides/ObjectMother rule (c51). Add it to Test-ChangeRisk plus one GENERAL trap line, like `WorkOrder Number max 7`.
