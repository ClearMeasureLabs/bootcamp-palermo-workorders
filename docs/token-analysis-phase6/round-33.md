# Round 33 — v46g (8991de26)

Tokens = transcript bytes/4 (`scripts/agent/measure_transcript.py` method via `/workspace/r28/an33.py`); pre-edit = `scripts/agent/measure_exploration.py`. Real CI `gh pr checks`: every PR **14 pass / 2 skipping** (copilot ignored). Delivery from each PR's files/diff vs its issue.

## Summary

| metric | value |
|---|---|
| items | c89–c96 → PRs 10811, 10809, 10806, 10810, 10812, 10807, 10805, 10808 |
| real CI GREEN | 8/8 |
| correctly delivered | 8/8 (c93 minimal: greeting reworded) |
| total / avg / median | 3,987.5k / 498.4k / 343.1k |
| without c93 | 2,474.4k / 353.5k / 239.7k |
| pre-edit (exploration) | 484.5k = 12.2% of total; avg 60.6k, median 63.05k; 8-12 calls before first edit |
| CI RED | 1 (c89 Qodana RedundantNameQualifier) |
| waits / CI cycles / wait-turn tokens | 14 / 9 / 324.1k |
| get_mcp_tools | 5 (c89, c90, c94, c95, c96), all issued in parallel with the first GENERAL.md read (started before it returned) |
| lost tool results (Submit/Wait) | 4: c91 Wait + c92 Submit both 08:43:03 CDT, c93 Wait 08:53:53 CDT; plus 2 instant `pwsh -Command Select-String` nulls (c92, c93) |
| 10-min cuts / background / terminal files | 0 / 0 / 0 |

## Per item

| id | PR | cat | tokens (k) | pre-edit (k) | calls before edit | calls | Submits | waits | wall (min) | mcp | delivered | notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| c89 | 10811 | UI | 790.5 | 75.9 (10%) | 12 | 44 | 11 | 3 | 42.6 | 1 | yes | "Unassigned" span + css; recipe ui-bunit-search printed and tests appended to WorkOrderSearchTests.cs as the NOTE said. 11 Submits: WorkOrderSearchResult/Row type, ctor, read-only props, own stub IBus missing `using MediatR`, QODANA_RISK, INSPECT=FIX; **CI RED** Qodana RedundantNameQualifier on untouched lines 677/681 made redundant by its new `using` |
| c90 | 10809 | UI | 239.7 | 67.0 (28%) | 10 | 20 | 2 | 1 | 17.6 | 1 | yes | "Attachments (N)" + "No attachments yet." for saved orders; Playwright in WorkOrderAttachmentTests |
| c91 | 10806 | multi-layer | 222.3 | 77.2 (35%) | 11 | 21 | 1 | 2 | 17.0 | 0 | yes | WorkOrdersCreatedInLastDaysQuery (1-30 validator, newest first) + `list-recent-work-orders` days=7; lost Wait 08:43:03 |
| c92 | 10810 | multi-layer | 596.4 | 59.1 (10%) | 8 | 32 | 3 | 1 | 23.3 | 0 | yes | Unassign AssignedToDraftCommand, MCP + ReferenceResources; wide ACC `(WorkOrders/*,McpServer/*) Passed: 77`; INSPECT=FIX MemberCanBePrivate on `Name` (same finding as c83 in r32); lost Submit 08:43:03 → rerun `CACHED:` + PUBLISH=NO_CHANGE |
| c93 | 10812 | vague | 1513.1 | 39.3 (3%) | 10 | 88 | 3 | 2 | 30.8 | 0 | yes | **outlier**: "Welcome X!" → "Signed in as **X**!" + tooltip in Logout.razor (visible, minimal). Read budget ran out before MainLayout was read → 41 probe search_replace calls (32 null results) = 472.2k; then changing the shared greeting broke many asserted strings → 37 calls of test fixes across 8 test files = 1,001.6k; lost Wait 08:53:53 |
| c94 | 10807 | bug | 88.6 | 37.6 (42%) | 9 | 14 | 1 | 2 | 18.9 | 1 | yes | Status sort uses SortBy; existing sort tests updated to workflow order; 1 Submit (ui-bunit-search) |
| c95 | 10805 | test-only | 90.3 | 45.7 (51%) | 10 | 14 | 1 | 1 | 15.7 | 1 | yes | CanConnectToDatabaseHealthCheck integration tests only; final reply lacked RESULT line though Wait printed CHECKS=GREEN |
| c96 | 10808 | trap | 446.6 | 82.7 (19%) | 10 | 28 | 1 | 2 | 18.5 | 1 | yes | BudgetCode max 12, DbUp 033, form + MCP; ClampLength in BogusOverrides before first Submit (LENGTH_RISK 0) |

## v46 effects

| change | effect |
|---|---|
| `ui-bunit-search` recipe + append NOTE | Fired on c89 and c94. c94: 1 Submit, 88.6k. c89: NOTE followed (tests in WorkOrderSearchTests.cs, no DI/auth errors as in c82), but still 11 Submits (c82: 12) — failures moved to building `WorkOrderSearchResultRow` data and a custom IBus stub, plus a Qodana CI RED. The recipe shows context setup, not how to feed custom search results. |
| per-stage Submit stamps | 0 stage hits (no UNIT/IT/ACC/INSPECT=CACHED). The only lost Submit (c92) had finished, so the whole-tree `CACHED:` path handled it (1 hit). |
| Get-Context `-File <dir>` listing | 0 uses (no agent passed a folder). |
| wide state-command acceptance (v44) | Fired on c92, 77 passed. |
| LENGTH_RISK (trap c96) | 0: agent clamped in BogusOverrides first (third round running). |

## Why c93 was long (1,513.1k, 88 calls, 30.8 min)

1. 8 Get-Context calls (7 `-Find`, 1 `-File`) hit the read budget before MainLayout.razor was read; the `-File MainLayout.razor` call itself returned only the budget message.
2. Its `pwsh -Command Select-String` read returned null; GENERAL forbids shell reads, so it **probed the file with search_replace** (no-op/class swaps to discover markup): 41 calls, 32 null results, 472.2k.
3. It changed the shared "Welcome" greeting asserted by AcceptanceTestBase and many tests; a bulk rename produced names like `ShouldDisplaySigned in asMessage…`, then 37 calls (1,001.6k) of test fixes, ACC_TEST=FIX once, 3 Submits.

Pre-edit tokens were the lowest of the round (39.3k, 3%): exploration was cut off, and the cost moved into blind edits.

## vs round 32

| | r32 (v45g) | r33 (v46g) |
|---|---|---|
| avg / median | 402.2k / 416.6k | 498.4k / 343.1k |
| total | 3,217.4k | 3,987.5k |
| pre-edit share | 605.4k (18.8%) | 484.5k (12.2%) |
| CI RED | 0 | 1 |
| get_mcp_tools | 1 | 5 |
| delivered | 8/8 | 8/8 |

## Candidates for after the control round (not built)

- Read budget: when it runs out with a named target file never read (c93), allow one `-File` read of it, or say "read the file with -File" instead of "edit now"; GENERAL "never probe with edits" did not hold.
- Local inspect: when a change adds a `using`, check RedundantNameQualifier on the whole file, not only changed lines (c89 CI RED).
- Search recipe: add an example that feeds custom results (existing test with a result-returning IBus and WorkOrderSearchResultRow).
- State-command recipe/trap: `Name` constant visibility (MemberCanBePrivate on c83 and c92).
- get_mcp_tools parallel with the first read: prompt-only ban not sufficient (5/8).
