# Phase 6 round 7 (v20g, ba0c0033): y01-y08

Measured with `measure_transcript.py` + `attribute_transcript_tokens.py`, bytes/4. 8/8 green.

| Item | Issue | PR | Input | Output | Calls | Get-Context | Reads | Submits | Waits |
|---|---|---|---|---|---|---|---|---|---|
| y01 | #10349 | 10359 | 71.5k | 0.78k | 15 | 2 | 7 | 1 | 1 |
| y02 | #10350 | 10357 | 26.1k | 0.47k | 11 | 1 | 3 | 1 | 2 |
| y03 | #10351 | 10361 | 92.0k | 0.58k | 14 | 2 | 5 | 1 | 2 |
| y04 | #10352 | 10363 | 376.0k | 1.90k | 31 | 3 | 17 | 2 | 2 |
| y05 | #10353 | 10364 | 489.5k | 1.71k | 34 | 13 | 11 | 1 | 1 |
| y06 | #10354 | 10358 | 40.2k | 0.61k | 11 | 1 | 3 | 1 | 1 |
| y07 | #10355 | 10360 | 18.0k | 0.58k | 7 | 1 | 2 | 1 | 1 |
| y08 | #10356 | 10362 | 60.7k | 0.57k | 13 | 2 | 5 | 1 | 2 |
| **Total** | | | **1,174.0k** | **7.20k** | 136 | 25 | 53 | 9 | 12 |

Per run: **146.8k**. Without y04/y05: 308.5k / 6 = **51.4k**, the best typical-run figure so far. y07 (18.0k) is the cheapest run ever.

## Comparison (input per run)
| Reference | Per run | Round 7 (146.8k) vs it |
|---|---|---|
| Round 6 (v19g) | 65.5k | +124% |
| Round 5 / 4 / 3 | 82.4k / 155k / 72k | +78% / -5% / +104% |
| Round 1 / 2 | 296k / 378k | -50% / -61% |
| Phase 4 rep 3 / Phase 5 | 97k / 99.5k | +51% / +48% |
| Original baseline | 392k | -63% |

## Attribution (all 8)
| Source | Tokens | Share |
|---|---|---|
| Reads/search before first edit (incl. Get-Context) | 839.9k | 71.5% |
| Assistant msgs/tool args | 167.2k | 14.2% |
| Skill file | 68.7k | 5.9% |
| Edits | 52.8k | 4.5% |
| Prompt | 24.5k | 2.1% |

Wait-call turns replay 124.8k (10.6%). In 4 runs the wait call hit the terminal timeout (null result) or ended PENDING and was run again.

## y04 / y05
- **y05 (489.5k, 42% of the round):** made 13 Get-Context calls, ignoring "at most one more call" (the skill rule is not enforced). It cycled `-Find` patterns (`WorkOrder.*Query`, `ByAssignee|ByStatus`, `WorkOrderByNumberQueryHandler`, `QueryValidator`...) looking for a complete query + handler + validator + test exemplar, and read RemotableRequestTests and BogusOverrides along the way.
- **y04 (376.0k):** made 17 reads tracing how an MCP filter reaches the data layer (WorkOrderTools, TestHost, McpWorkOrderToolTests 6.8 KB, WorkOrderSpecificationQuery, WorkOrderSearchSpecification, WorkOrderQueryFilters, plus greps). It also ran `gh issue view` and had one RISK=FIX resubmit.
- **Common cause:** for "a new kind of thing" (new query, new MCP parameter), Get-Context shows where the code lives but not a complete exemplar of the change, so agents assemble one by searching.

## v20g evaluation
- **Relevance ranking + bigger FULL:** these work for single-file changes. y02 (Role.cs and RoleTests FULL) came in at 26.1k, y07 at 18.0k, and y06 at 40.2k, each with 1 Get-Context call and 2-3 reads.
- **Sibling fallback:** found the right tests. But FetchData.razor (109 lines) and WorkOrderStatus.cs are over the 90-line FULL limit, so y03 and y08 still read them whole.
- **Shorter GENERAL.md:** the skill share fell to 68.7k (5.9%) from 79.1k (15.1%), even with 37 more calls this round.

## v21g (targets reads before first edit, 71.5%)
1. **RECIPES:** `scripts/agent/recipes.txt` holds general exemplars of change kinds, printed in full by `Get-Context -Issue` when the issue text matches. `new-query` (query/handler) is the 5-file CountByStatus query + validator + handler + validator test + integration test set. `mcp-tool-param` (MCP) is the ListWorkOrders signature + McpWorkOrderToolTests 1-63. When a recipe prints, the outline shrinks to 1 production + 1 test file.
2. **Hard 2-call limit:** Get-Context counts its calls in a temp file, and a third call prints `LIMIT:` instead of searching.
3. The skill mentions RECIPE and LIMIT (1,739 bytes).
Box check: y05's issue now prints the full query recipe (7.1 KB output, 1 call), y04's prints the MCP recipe (6.4 KB), and y02 is unchanged (2.1 KB).
