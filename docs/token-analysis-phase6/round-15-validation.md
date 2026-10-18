# Phase 6 round 15: validation of frozen v28g on unseen kinds of work (u01-u08, -v28)

Skill frozen at v28g (analysis/token-reduction, no comment refactor). Items #10493-#10500 were chosen to differ in kind from every Phase 6 item (no Employee methods, Counter, word-count, username rules or list-* filters). Measured exactly as round 14 (`measure_transcript.py` logic, `attribute_transcript_tokens.py`; bytes/4). 8/8 green, no CI RED.

| Item | Kind | Issue | PR | Transcript | Input | Calls | Get-Context | Reads/greps before edit | Submits | Waits |
|---|---|---|---|---|---|---|---|---|---|---|
| u01 | WorkOrder.GetTimeToComplete (domain) | #10493 | 10501 | bc-322dc2ca | 22.3k | 8 | 1 | 2 | 1 | 1 |
| u02 | business-days endpoint (new API) | #10494 | 10506 | bc-1c7de422 | 102.7k | 15 | 2 | 7 | 1 | 1 |
| u03 | Settings theme status (Blazor, new page) | #10495 | 10505 | bc-a2cf6d14 | 33.1k | 9 | 1 | 0 (+1 mid-edit) | 1 | 1 |
| u04 | Role.Description column (DbUp) | #10496 | 10502 | bc-3062c1d5 | 38.6k | 10 | 2 | 1 | 1 | 1 |
| u05 | attachments newest first (MCP, other tool) | #10497 | 10507 | bc-ddd9ee8d | 68.3k | 12 | 2 | 4 (+1 mid-edit) | 1 | 1 |
| u06 | attachment metadata validator | #10498 | 10504 | bc-18d7a2bf | 55.0k | 11 | 2 | 4 | 1 | 1 |
| u07 | trim number in WorkOrderQueryHandler (bug fix) | #10499 | 10503 | bc-052d581c | 40.2k | 10 | 1 | 2 | 1 | 2 (1 empty) |
| u08 | search header singular + acceptance test | #10500 | 10508 | bc-77975c11 | 309.6k | 16 | 2 | 5 (+2 mid-edit) | 1 | 1 |
| **Total** | | | | | **669.8k** | 91 | 13 | 25 (+4) | 8 | 9 |

- **Per run:** 83.7k. Median 47.6k; max 309.6k (u08); min 22.3k (u01). Without u08: 360.2k, 51.5k/run.
- **Output:** 6.13k total (0.53 / 0.92 / 0.59 / 0.52 / 0.77 / 0.74 / 0.71 / 1.07k).

## Comparison
| | Per run | This round vs it |
|---|---|---|
| Round 14 (v28g, familiar items) | 30.0k | +179% (median +59%; without u08 +72%) |
| Rounds 13-14 average (506.3k / 16) | 31.6k | +165% (without u08 +63%) |
| Original baseline | 392k | -79% |

## Attribution (all 8)
| Source | Tokens | Share |
|---|---|---|
| Reads/search before first edit (incl. Get-Context) | 470.3k | 70.2% (39.1% in round 14) |
| Assistant msgs/tool args | 70.0k | 10.4% |
| Skill file | 65.9k | 9.8% |
| Edits | 31.5k | 4.7% |
| Prompt | 19.2k | 2.9% |
| Reads after first edit | 10.9k | 1.6% |

Wait-call turns replay 94.1k (14.0%). No calls after GREEN in any run, every run took 1 Submit, and no RISK fired.

## Outliers and causes
- **u08 (309.6k, 46% of the round):**
  - One broad grep returned 87.4 KB of matches (mostly the ~700-line `WorkOrderSearchTests.cs`). It was replayed 8 times, about 174.8k on its own.
  - `WorkOrderSearch.razor` (10.0 KB) was read in full and replayed 12 times, about 30.0k. It is too big for Get-Context's FULL limit, and no recipe covers Blazor pages or acceptance tests.
- **u02 (102.7k):** no recipe for a new controller. It used 7 reads/greps (an existing tools controller and its tests, a 4.4 KB grep, 2 globs). It also read ApiKeyAuthenticationMiddleware twice while hunting for how anonymous routes are allowed; the skill names `ApiPublicPathRules.PublicLeaves` but Get-Context doesn't show it.
- **u05 (68.3k) and u06 (55.0k):**
  - u05: the MCP recipe covers query parameters, not result ordering. Agents read WorkOrderTools, WorkOrderAttachment and a 5.7 KB grep, then McpWorkOrderToolTests mid-edit.
  - u06: a brand-new validator for a command with no validator yet, so 2 greps and 2 look-alike validator reads.
- **Well-covered kinds stayed cheap:** u01 domain (22.3k), u03 Blazor on a small page (33.1k), u04 DbUp (38.6k), u07 handler fix (40.2k). That is the same range as round 14's items (12.5-42.0k).

## Does v28g's average hold on unseen work?
**No.**
- Kinds covered by recipes or small FULL files land at 22-40k, close to the 30k average.
- Kinds with no recipe and big files run 55-310k: new controllers, large Blazor pages with acceptance tests, MCP changes beyond parameters, new validators.
- The rounds 1-14 gains partly fitted the repeated item shapes.

The main remaining risk is unbounded grep/read output. One unbounded grep alone cost more than five average runs.

**If iteration resumes, the next levers are:**
- Cap built-in grep/read output (e.g. steer greps through Get-Context `-Find`, which is capped).
- Print line ranges of large pages instead of whole files.
- Add recipes for a new API controller (including PublicLeaves) and for a Blazor page + acceptance test.
