# Phase 6 round 12 (v25g, 1e02cd43): s01-s08 (k12)

Measured with `measure_transcript.py` + `attribute_transcript_tokens.py`, bytes/4. 8/8 green (s03 checked directly on the PR; its agent ended SCRIPT_ERROR from a Submit bug).

| Item | Type | Issue | PR | Transcript | Input | Output | Calls | Get-Context | Reads (excl. skill) | Submits | Waits |
|---|---|---|---|---|---|---|---|---|---|---|---|
| s01 | domain | #10429 | 10438 | bc-d7e75841 | 32.4k | 0.55k | 9 | 2 | 1 | 1 | 1 |
| s02 | UI/bUnit | #10430 | 10439 | bc-0130fd33 | 35.9k | 0.63k | 10 | 1 | 0 | 2 (Qodana RED) | 2 |
| s03 | MCP | #10431 | 10437 | bc-30bbdd81 | 64.7k (48.2k to 1st Submit) | 0.89k | 12 | 2 | 4 | 3 (script bug) | 0 |
| s04 | query | #10432 | 10440 | bc-4f846301 | 62.2k | 0.58k | 13 | 2 | 2 | 1 | 1 |
| s05 | API JSON | #10433 | 10443 | bc-efbc4877 | 125.4k | 1.17k | 20 | 2 | 3 | 2 (Qodana RED) | 6 |
| s06 | validation | #10434 | 10441 | bc-b42b0e80 | 36.0k | 0.60k | 12 | 2 | 4 | 1 | 1 |
| s07 | DB column | #10435 | 10442 | bc-eaf05fea | 49.9k | 0.57k | 12 | 2 | 3 | 1 | 1 |
| s08 | bug fix | #10436 | 10444 | bc-4c63bb39 | 27.0k | 0.56k | 9 | 1 | 1 | 2 (TEST=FAIL) | 1 |
| **Total** | | | | | **433.5k** | **5.55k** | 97 | 14 | 18 | 13 | 13 |

Per run: **54.2k** (round 11: 54.9k). Without s03's script-bug overhead (counting s03 only up to and including its first Submit turn, 48.2k; the turns after it were the bug's aborted/rerun Submits and the resume), the total is 417.0k, **52.1k per run**. A normal s03 would also have had one wait turn, so the real figure is a little higher.

## Comparison (input per run)
| Round | Skill | Per run | Round 12 vs it |
|---|---|---|---|
| 11 | v24g | 54.9k | -1% |
| 10 | v23g | 77.7k | -30% |
| 9 | v22g | 245.1k | -78% |
| 8 | v21g | 364.6k | -85% |
| 7 | v20g | 146.8k | -63% |
| 6 | v19g | 65.5k | -17% |
| original baseline | | 392k | -86% |

## Attribution (all 8)
| Source | Tokens | Share |
|---|---|---|
| Reads/search before first edit (incl. Get-Context) | 219.0k | 50.5% (56.3%) |
| Assistant msgs/tool args | 77.0k | 17.8% |
| Skill file | 61.9k | 14.3% |
| Edits | 42.3k | 9.7% |
| Prompt | 19.3k | 4.5% |
| Wait results / submit results | 14.0k | 3.2% |

Wait-call turns replay 89.2k (20.6%; 12.7% in round 11), mostly s05: 6 waits (a cancelled Acceptance job, auto-rerun, then a Qodana RED).

## Did v25g hold?
- **Lambda fix:** yes. No CRAP_RISK in any run (round 11 t07 lost about 40k to a false one). s07 (the DB-column/EF mapping item) took 1 Submit.
- **Script fixes during the round:** s03's Submit crashed on a NO_CHANGE republish ("Cannot index into a null array."). Root cause: the v19g head-sync line piped `($null).Matches` into ForEach. Fixed in 1e5e828e. The crash handler in 9d99b82c (prints `SCRIPT_ERROR <script>:<line>`, exits 0) is what pinpointed it on the second run. s03 cost 16.5k extra.

## Top remaining waste
1. **Second Get-Context call proving a name is new (6 of 8 runs, 2.5-3.8 KB each, replayed 8-15 times):** `-Find 'GetInitials|...'`, `'averageWordLength|...'`, `'WorkOrderCountByAssigneeQuery|...'`, `'OfficeLocation|...'`.
2. **Re-reading the sibling test (4 of 8 runs):** a long test file was only outlined, so agents read it to copy a test body and find the end of the class (EmployeeTests 230 lines in s01/s08; s03 got TestHost.cs instead of McpEmployeeToolTests; s05 ToolsWordCountControllerTests).
3. **No test to copy for a validator (s06):** 3 greps plus a read to find a validator test.
4. **Two Qodana REDs (s02 RedundantNameQualifier `new Microsoft.AspNetCore.Components.Web.MouseEventArgs()`, s05 RedundantUsingDirective `using System.Linq;` under ImplicitUsings):** each cost an extra Submit plus a wait, and s05 also had a 30-minute CI cycle.
5. **DbUp number hunt (s07):** a glob of scripts/Update.

## v27g
- **Get-Context:**
  - `NEW (not in src yet; don't search for them): X` lists backticked issue names found nowhere in src.
  - `NEXT_SCRIPT=src/Database/scripts/Update/033_<Name>.sql` appears for DbUp/column issues.
  - A long sibling test prints its usings and its last test in full ("append after it"), outside the cap.
  - The sibling test lookup also matches `*EmployeeTool*Tests.cs`, then falls back to the smallest test in the mirror UnitTests folder (validators).
- **Test-ChangeRisk:** QODANA_RISK for added namespace-qualified `System.*`/`Microsoft.*` type uses, and for `using System[.Linq|.IO|.Collections.Generic|.Net.Http|.Threading[.Tasks]];` in ImplicitUsings projects.
- **GENERAL.md:** use NEW, NEXT_SCRIPT and the test tail; both Qodana traps added (2.6 KB).
- **Box checks:**
  - The round 12 issues give: NEW for s01, s04, s05, s07; s03's sibling test is McpEmployeeToolTests with its tail; s06 gets WorkOrderCountByStatusQueryValidatorTests FULL.
  - Replaying s02's and s05's lines gives both QODANA_RISKs.
  - The round 13 issues give NEW for r01, r03, r04, r05, r07, and NEXT_SCRIPT 033 for r07.
