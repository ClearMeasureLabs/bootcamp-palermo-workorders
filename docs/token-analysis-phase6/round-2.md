# Phase 6 round 2 (v15g, 84d8d07d): 8/8 green, input up again, caused by a v15g script bug

| Item | Issue | PR | Input | Output | Calls |
|---|---|---|---|---|---|
| s01 GetAgeInDays | #10270 | #10281 | 232.9k | 2.95k | 25 |
| s02 user name length | #10271 | #10280 | 262.5k | 2.95k | 31 |
| s03 Counter decrement | #10272 | #10279 | 265.4k | 2.34k | 30 |
| s04 attachments newest first | #10273 | #10284 | 545.5k | 4.02k | 42 |
| s05 content type 255 | #10274 | #10285 | 977.1k | 6.08k | 54 |
| s06 AI URL validation | #10275 | #10282 | 215.1k | 2.40k | 29 |
| s07 get-employee trim | #10276 | #10278 | 247.7k | 2.62k | 29 |
| s08 GUID uppercase | #10277 | #10283 | 278.5k | 3.08k | 32 |
| **Total** | | | **3,025k** | **26.4k** | 272 |

Per run 378k (round 1: 296k; phase 4 rep 3: 97k; phase 5: 99.5k).
Attribution: reads before first edit 905k (30%), assistant messages 702k (23%), reads after first edit 687k (23%),
skill 193k (6%), edits 193k (6%), submit 124k (4%), get_mcp_tools 115k (4%); wait turns 363k (12%).

## Diagnosis
- **v15g bug broke Submit-Change in all 8 runs.** v15g added `Set-Location "$PSScriptRoot/../.."` to Submit-Change and
  Test-ChangeRisk, but Submit runs the risk check from its temp copy (/tmp/agent-scripts), so the path resolved to `/`,
  `git` failed ("Could not access origin/master") and the build couldn't find the project. My box test ran the scripts in
  place, not from the temp copy, so it missed this. Every agent then debugged: git refs (fetch, update-ref, packed-refs),
  reading Submit-Change, Publish-AgentPr, Invoke-AgentBuildTest and Test-ChangeRisk, and finally worked around it by setting
  TMPDIR to a folder inside the repo. That is why even one-line items (s02) cost 215-280k.
- **s05 (977k):** the same debugging plus symlinks/sudo/safe.directory attempts, then a 12.6 KB manual SQLite test run, an
  integration failure investigation and an edit to an unrelated integration test file.
- **s04 (546k):** the same debugging plus 5 Get-Context calls (3 at the 4.2 KB cap) and two glob searches before editing.

## Did v15g fixes hold?
- Missing-output digging after Submit: no silent Submit outputs this round (the inline wait was gone), but the script bug
  caused far worse digging.
- Get-Context size: capped at about 4.2 KB per call, but runs made 2-5 calls (rule says at most 2).
- RISK=FIX false positives: none seen (the risk check never ran correctly until the TMPDIR workaround).
- Scope creep: no controller/caller rewrites like r03; s05 edited an unrelated test.
- get_mcp_tools: once in each of the 8 runs (115k).

## v16g changes
- Get-Context, Test-ChangeRisk, Submit-Change locate the repo with `git rev-parse --show-toplevel` (works from the temp
  copy and from any cwd). Tested on the box by running Submit-Change -VerifyOnly from `src/` via the temp copy: risk check
  flagged a planted branch; with -IgnoreRisk, unit tests + changed integration class (SQLite) passed (VERIFY=OK).
- Get-Context cap 3.5 KB -> 2.5 KB.
- GENERAL.md: a script failure not caused by your code -> reply `SCRIPT_FAIL: <last line>` and stop; never debug scripts,
  git refs, terminal files or the PR (turns a script bug into a cheap, visible failure instead of 200k+ of debugging).
