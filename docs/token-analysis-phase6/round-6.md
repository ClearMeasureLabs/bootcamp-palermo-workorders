# Phase 6 round 6 (v19g, 2529c7ce): x01-x08

Measured with `measure_transcript.py` + `attribute_transcript_tokens.py`, bytes/4. 8/8 green.

| Item | Issue | PR | Input | Output | Calls | Get-Context | Reads | Submits | Waits |
|---|---|---|---|---|---|---|---|---|---|
| x01 | #10333 | 10341 | 42.0k | 0.57k | 11 | 2 | 4 | 1 | 2 |
| x02 | #10334 | 10342 | 45.1k | 0.50k | 12 | 2 | 5 | 1 | 1 |
| x03 | #10335 | 10347 | 55.3k | 0.53k | 12 | 2 | 4 | 1 | 1 |
| x04 | #10336 | 10346 | 175.0k | 1.27k | 20 | 2 | 12 | 1 | 1 |
| x05 | #10337 | 10348 | 123.5k | 1.26k | 18 | 3 | 7 | 1 | 2 |
| x06 | #10338 | 10345 | 23.3k | 0.55k | 8 | 1 | 3 | 1 | 1 |
| x07 | #10339 | 10343 | 39.2k | 0.56k | 10 | 2 | 3 | 1 | 1 |
| x08 | #10340 | 10344 | 20.8k | 0.41k | 8 | 1 | 3 | 1 | 1 |
| **Total** | | | **524.1k** | **5.65k** | 99 | 15 | 41 | 8 | 10 |

Per run: **65.5k**, the best round so far. Reads include the skill file.

## Comparison (input per run)
| Reference | Per run | Round 6 vs it |
|---|---|---|
| Round 5 (v18g) | 82.4k | -21% |
| Round 4 (v17g) | 155k | -58% |
| Round 3 (v16g) | 72k | -9% |
| Round 1 / 2 | 296k / 378k | -78% / -83% |
| Phase 4 rep 3 / Phase 5 | 97k / 99.5k | -32% / -34% |
| Original baseline | 392k | -83% |

## Attribution (all 8)
| Source | Tokens | Share |
|---|---|---|
| Reads/search before first edit (incl. Get-Context) | 311.1k | 59.4% |
| Assistant msgs/tool args | 85.9k | 16.4% |
| Skill file | 79.1k | 15.1% |
| Edits | 25.8k | 4.9% |
| Prompt | 17.8k | 3.4% |

Wait-call turns replay 76.8k (14.6%).

## v19g evaluation
- **Every first Submit passed** (8 submits / 8 runs, compared with 16 in round 5): no RISK, no local failures, no CI RED. The validator and nullable traps held (x05 added its validator on the first try).
- **FULL small files:** FULL appeared in 5 runs (11 files). But the files agents actually edit (WorkOrder.cs 119 lines, EmployeeTools.cs 52, Counter.razor 67) and their sibling tests were over 35 lines, so they were outlined and then read whole anyway.
- **Tests sibling:** this worked when named `<Name>Tests.cs`, but missed `McpEmployeeToolTests` and `CounterPageTests`. Ranking by path length put `ServerApplication.cs` and `TestHost.cs` ahead of `EmployeeTools.cs` (x04).
- **Exit-0 failure output / head-sync wait:** not exercised. There were no local failures and no re-pushes, so these are untested in the field.
- **Waits:** x01 needed a second wait because CI took longer than ~17 min. In x05 a wait hit the terminal timeout (null result) and was rerun.
- **x04 outlier (175.0k):** after 2 Get-Context calls it made 12 reads/searches (two greps of 6.9 KB and 12.9 KB on role loading, plus WorkOrderTools.cs read for a filter pattern). It also ran a stray `git checkout`.

## v20g (targets reads before first edit, 59%, and the skill file, 15%)
1. Get-Context ranks files by relevance: file name is a term > declares the type or MCP tool name > mentions it. Mention-only files go in an `ALSO` line instead of being outlined, and Generated/ is skipped.
2. The top production file is printed FULL up to 90 lines and the sibling test up to 60 lines (other files 35). The cap is 5.5 KB.
3. Sibling test fallback: a test file that references the class (EmployeeTools -> McpEmployeeToolTests). AcceptanceTests are excluded.
4. "the X page" in an issue maps to `UI.Shared/Pages/X.razor`. A brand-new type with no matches falls back to the EXISTING look-alikes (the pattern to copy).
5. GENERAL.md went from 2,971 to 1,645 bytes (-45%). The redundant map was dropped (Get-Context now finds the files), and `gh`/`dotnet` are on the never list.
Box check on round-6 issues: x04 now gets EmployeeTools.cs FULL as the first file, x03 gets Counter.razor and CounterPageTests.cs FULL (both full files in 1 call), and x05 gets the Employee handler and validator look-alikes FULL.
