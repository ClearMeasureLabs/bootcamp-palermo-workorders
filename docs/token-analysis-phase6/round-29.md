# Round 29 — v42g (c21e7999), c57-c64 (#10733-10740), 1 item/session

Method: same as rounds 27/28. `measure_transcript.py` (bytes/4 over the full input replayed at every turn that records a tool call). Real CI comes from `gh pr checks`, ignoring the copilot reviewer. Durations come from `duration_ms` in the transcripts.

| id | category | PR | input | calls | Submits | CI REDs | WAIT_CMD calls | Submit calls cut at 10.0 min | wall | real CI | PR content |
|---|---|---|---|---|---|---|---|---|---|---|---|
| c57 | UI (duplicate) | 10748 | 1,395.6k | 54 | 15 | 0 | 0 | 1 (of 2 publishing) | 50 min | GREEN (14 pass, 2 skipping) | feature + bUnit + Playwright |
| c58 | UI (file sizes) | 10747 | 238.7k | 27 | 2 | 0 | 0 | 1 | 20 min | GREEN | formatter + unit tests (no new bUnit/Playwright test) |
| c59 | multi-layer (attachment description) | 10745 | 692.2k | 35 | 3 | 0 | 0 | 1 | 20 min | GREEN | 12 files, complete |
| c60 | multi-layer (completion notes) | 10744 | 3,488.5k | 114 | 7 | 0 | 0 | 3 | 68 min | GREEN | **deletes 403 lines of the existing `WorkOrderMappingTests.cs`** |
| c61 | vague (printing) | 10742 | 56.5k | 14 | 1 | 0 | 0 | 1 | 16 min | GREEN | **no product change** (only the ServerFixture hook) |
| c62 | symptom bug (overdue filter) | 10741 | 70.3k | 12 | 1 | 0 | 0 | 1 | 16 min | GREEN | **no product change** (only the ServerFixture hook) |
| c63 | test-only (shelve) | 10743 | 219.6k | 19 | 3 | 0 | 0 | 0 (15.2 min, full output) | 19 min | GREEN | new integration test class |
| c64 | trap (due before) | 10746 | 785.7k | 37 | 3 | 0 | 0 | 0 (16.4 min, full output) | 20 min | GREEN | 11 files, Bogus rule added |

- **Total:** 6,947.1k
- **Average:** 868.4k/item; 493.0k without c60
- **Median:** 465.4k
- **Round 28 comparison:** average 1,689.4k, median 727.3k.
- **Results:** 8/8 real GREEN and 8/8 RESULT lines. However, **only 5/8 PRs are a correct delivery**: c61 and c62 changed nothing, and c60 deleted an existing test file's contents.

## Key findings
1. **v42g leaked the ServerFixture seeding hook into every PR**: `src/AcceptanceTests/ServerFixture.cs` +25 in all 8.
   - Publish-AgentPr diffs from `origin/analysis/token-reduction` when that ref exists, and otherwise from `origin/master`.
   - The hook (v42g's only `src/` change on the analysis branch) still reached every PR, so the patch was taken against a base that lacks it.
   - The same leak turned **c61 and c62 into false GREENs**. Neither agent edited anything: Submit printed `INSPECT=SKIPPED no changed .cs` and `ACC_TEST=SKIPPED no UI/acceptance change`. Even so, the hook made the patch non-empty, so `NO_CHANGES` never fired, and a PR containing only the hook went green.
2. **Why c61 and c62 edited nothing:** Get-Context refused their reads.
   - Both used the first call plus 3-4 `-Find` calls. Their unranged `-File 'a.razor,b.razor.cs'` call then got `NEXT: read budget used up`. An unranged `-File` after 3 calls is not allowed.
   - With no file content, both agents went straight to Submit.
   - c62 retried the same call once, and then probed `/tmp/agent-*`/`scripts/agent` with `ls`.
3. **Submit hit the terminal's ~10-minute cap.**
   - 9 of the 12 Submit calls that ran past the inline-wait threshold ended at **exactly 10.0 min** with partial or empty output; 3 ran to 15.0-16.4 min and returned full output.
   - After a cut, agents waited with an AwaitShell-style tool that the transcript does not record as a tool row. They then read the terminal file (`/home/ubuntu/.cursor/projects/workspace/terminals/*.txt`), which GENERAL.md forbids.
   - Because those wait turns have no recorded tool call, `measure_transcript.py` does not count them. A heuristic count (assistant turns with no tool call that mention waiting, followed by another assistant turn) finds 27 such turns, worth about 675.6k of replay. This figure is **outside the method and approximate**.
   - The 20-min single wait from v42g therefore usually did not work as a single call. Round 28's 19.4-min Wait call was the exception, not the rule.
4. **CI REDs:** 0 (round 28: 5).
   - FAILED_TESTS (the no-flake-rerun path) **never fired**, because no PR went RED. FLAKE-RERUN: 0.
   - There were 0 WAIT_CMD calls: every verdict arrived inside Submit, or through the backgrounded Submit.
5. **Local acceptance catches:**
   - `ACC_TEST=FIX` 5 times, all on c57. That is the main reason c57 needed 15 Submits; the other causes were 6 `SUBMIT=FAIL` on unit tests and builds.
   - `ACC_TEST=OK` on c57, c58, c59 and c64.
   - `SKIPPED` on c61, c63 (no UI change) and c62.
   - c60's ACC lines were lost in its 10-min cut outputs.
   - The wide WorkOrders+McpServer run does not show as triggered in any recorded output.
6. **LENGTH_RISK:**
   - **Fired once, on c60** (`CompletionNotes`). The agent then added a `ClampLength` rule to BogusOverrides (+1 line in the PR).
   - It **missed c59's** `WorkOrderAttachment.Description` HasMaxLength(500), because the rule only checks for any `.Description =` in BogusOverrides, and `order.Description = …` (the WorkOrder case) already exists. The PR still went green.
7. **Other:**
   - `INSPECT=FIX` once (c58).
   - c64 (trap) added the `BogusOverrides`/`ObjectMother` rule for the new `DateOnly?` query property, and went green on its first CI cycle.
   - `get_mcp_tools` was still called 1-2 times in all 8 items.

## Outliers
- **c60, 3,488.5k (114 calls, 88 edits, 7 Submits):**
  - The read budget ran out on its 4th Get-Context call. It then needed the mapping-test pattern (`EmployeeMappingTests`) and could not read it.
  - It went through LENGTH_RISK, a QODANA_RISK (unused using), IT_BUILD=FAIL (wrong DataContext constructor) and a duplicate `[Test]` build error.
  - 3 Submits were cut at 10 min.
  - The resulting PR replaces the existing 400-line `WorkOrderMappingTests.cs` with a small file (+16 -403) and still passes CI.
- **c57, 1,395.6k (15 Submits):** 6 `SUBMIT=FAIL` (unit tests and builds) and 5 `ACC_TEST=FIX` before the Playwright test passed locally. After that it went green on its first CI cycle.

## Proposed v43 (not built)
1. **Fix the publish base and empty changes:**
   - Take the patch base as the item branch's starting commit: `git merge-base HEAD origin/<Branch>`, or the HEAD recorded on Get-Context's first call. Do not use `origin/analysis/token-reduction`, which agent VMs may lack.
   - Fail with `NO_CHANGES` when the agent's own src diff is empty.
   - Move the seeding hook out of committed `src/`: Invoke-LocalAcceptance applies it as a temporary patch and reverts it after the run.
   - This would have stopped the c61/c62 false GREENs and the hook leak in all 8 PRs.
2. **Keep every terminal call under 10 min:**
   - Submit's inline wait budget goes back to about 9 min total per call, and WAIT_CMD to `-MaxCallMin 9`.
   - If Submit's own work exceeds 7 min, skip the inline wait and print WAIT_CMD.
   - This removes the 10.0-min cuts, the unrecorded await turns and the terminal-file reads (9/12 long Submits).
3. **Get-Context: an unranged `-File` after the extra calls returns the file outline** (or the first 120 lines) instead of NEXT, inside the existing line budget. c61, c62 and c60 all lost their only read of the target file.
4. **New risk rules:**
   - `TEST_DELETE_RISK` when an existing test file loses more than 100 lines or more than half its lines (c60 -403).
   - LENGTH_RISK checks the clamp inside the matching entity's `case` block in BogusOverrides (c59 false negative).
