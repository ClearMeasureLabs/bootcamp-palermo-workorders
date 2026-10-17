# Phase 2: Title max length 300 → 1000, repeated 20 times to full green CI

The same work item runs every iteration. A Cursor cloud agent drives it to a PR with **all CI checks green**, including the integration and acceptance builds. The dispatcher then closes the PR unmerged and closes the issue as not planned. Between iterations: token analysis plus skill/script improvements. Branch: `analysis/token-reduction` (never merged).

## Findings on master (2026-10-16)
Current Title limit: **300** (raised from 200 in update script 024). The room-number change (PR #8423, script 029, `WorkOrder.RoomNumberMaxLength = 900`) is the house precedent to copy.

| Layer | File | Today | Needed |
|---|---|---|---|
| Domain | `src/Core/Model/WorkOrder.cs` | no Title constant (`RoomNumberMaxLength = 900` sits at line 12) | add `public const int TitleMaxLength = 1000;` |
| EF mapping | `src/DataAccess/Mappings/WorkOrderMap.cs:21` | `HasMaxLength(300)` | `HasMaxLength(WorkOrder.TitleMaxLength)` |
| DB migration | `src/Database/scripts/Update/` (DbUp, `NNN_Name.sql`, latest **032**) | `024_ExtendWorkOrderTitleLength.sql` set 300 | new **`033_ExtendWorkOrderTitleLengthTo1000.sql`**: `ALTER TABLE [dbo].[WorkOrder] ALTER COLUMN [Title] NVARCHAR(1000) NOT NULL`, wrapped in the BEGIN TRAN / PRINT / @@ERROR / COMMIT template copied from 029 |
| UI model validation | `src/UI.Shared/Models/WorkOrderManageModel.cs:21` | `[Required]` only | add `[StringLength(WorkOrder.TitleMaxLength, ErrorMessage = "Title cannot exceed 1000 characters.")]` |
| UI edit screen | `src/UI.Shared/Pages/WorkOrderManage.razor:48` | `<InputText ... id="Title">`, no maxlength | add `maxlength="@WorkOrder.TitleMaxLength"` (see the RoomNumber field, line 84) |
| Command validation | `src/Core/Validation/CreateDatedWorkOrdersCommandValidator.cs:18` | `Title NotEmpty` | add `.MaximumLength(WorkOrder.TitleMaxLength)` |
| MCP tool | `src/McpServer/Tools/WorkOrderTools.cs` | clamps RoomNumber (line ~342); no Title clamp | optional: mirror the clamp/reject for Title |
| Test data | `src/UnitTests/BogusOverrides.cs:22` | `ClampLength(1, 200)`, a stale comment that still says 200 | `ClampLength(1, WorkOrder.TitleMaxLength)` |
| Integration tests | `src/IntegrationTests/DataAccess/Mappings/WorkOrderMappingTests.cs:245,271,284` | hard-coded 300/301 | use `WorkOrder.TitleMaxLength` (±1) |
| New unit tests | mirror `WorkOrderManageModelRoomNumberTests.cs` and `WorkOrderManageRoomFieldTests.cs` (bUnit maxlength attribute) | — | Title equivalents |
| Acceptance (optional) | mirror `src/AcceptanceTests/WorkOrders/WorkOrderRoomNumberLengthTests.cs` | — | 1000-char title round trip |

## CI (build.yml; master has no branch protection or required-check rules, so "full green" = every check-run on the PR head)
Detect code changes · Code Analysis · Qodana (Community .NET) + QDNETC · Integration Build ×4 (SQLite, ARM SQLite, SQL container, Windows LocalDB) · Acceptance Tests ×2 (default, ARM SQLite) · Build result · Publish Release Candidate · Publish to GitHub Packages · secret-scan · copilot-pull-request-reviewer. Octopus deploy and Security Scan are skipped on PRs. A recent green master PR took **~13 min** wall clock.

## Measurement (input vs output, separately)
No token fields exist in the transcripts. Estimates, bytes/4:
- **Input** = Σ over model turns (each assistant tool-call turn) of the transcript bytes preceding that turn. This is the replayed prompt + cache reads.
- **Output** = Σ bytes of assistant `thinking` + `text` + tool-call arguments.
- Also tracked: turns, tool calls by type, CI poll calls, CI red→fix cycles, wall clock.
- Script: `scripts/agent/Measure-Transcript.ps1` (to be added) or the jq/awk one-liners recorded in each run file.

## Technique ledger
| Technique | Introduced (iter) | Δ input | Δ output | Notes |
|---|---|---|---|---|
| (baseline: stock bootcamp feature loop) | 1 | ~1.68M (lower bound; 7 subagents uncounted) | ~12.4k | 60 parent calls, 0 CI reds, 1 review-fix cycle, 1 extra notification run |
| T1-T5 together (file map, canned change, one-call CI wait, local SQLite tests, no subagents/board/MCP) | 2 | -1.67M (-99.2%) | -10.6k (-85.5%) | 7 calls, 4 of them recovery from a path bug |
| v2 fix: scripts copied to temp before the branch switch | 3 | +84k vs iter 2 (masked by a CI flake) | +0.9k | the fix worked (no missing-script calls); the flake dominated |
| **T3 one-call CI wait**: removed in iter 4 | 4 | removing it: **+7.1k (+53%)** vs iter 2 | **-0.4k** | cheap on a green run (runtime sleep between polls); T3's value shows on reds/flakes |
| **T2 canned whole-change script**: removed in iter 5 | 5 | removing it: **+593k (~45x)** | **+1.3k (+70%)** | the dominant technique: 13 reads + 12 edits, each replaying context |
| Flake cost (iter 3, v2 wait without flake handling) | 3 | +84k | +0.9k | one CI flake = 7 diagnostic calls with 26 KB of log dumps plus an empty-commit retrigger |
| v3: Wait-PrChecks per-call cap 9 min (never backgrounded), auto-rerun of infra flakes, aggregator filtered | 9 | **~0.86k total** (-12.5k vs iter 2, -96.5k vs iter 3) | **~0.45k** (-1.35k vs iter 2) | 2 tool calls / 4 turns; best run so far: input **-99.95%**, output **-96.4%** vs baseline |
| **T1 file map**: removed in iter 6 (hand-edit path, vs iter 5) | 6 | removing it: **+577k (+95%)** | **+1.5k (+49%)** | matters only when the agent edits by hand |
| **T4 local SQLite IT**: removed in iter 7 (vs iter 9) | 7 | ~+0.1k (noise) | ~+0.04k (noise) | token-neutral on a green run; its value is catching mapping errors before ~13 min of CI |
| **T5 lean skill**: removed in iter 8 (vs iter 9) | 8 | removing it: **+692k (~800x)** | **+10.2k (~23x)** | the stock loop adds board, MCP and journal work even when scripts exist; the largest *output* lever |
| v4: terse prompt + "no narration" + one `RESULT` line | 10-11 | **~0.63k** (-0.23k, -27% vs iter 9) | **~0.31k** (-0.14k, -31%) | best clean run; one run was backgrounded at the ~600 s cap (+3.6k) |
| **Prompt terseness** removed (iter 13: v3 prompt on v4) | 13 | confounded by the publish bug (~33.6k) | ~2.2k | clean estimate = iter 9 vs 11: +0.23k input / +0.14k output |
| v4 generic field script (`-Field RoomNumber -Max 1200`) | 12 | ~0.64k (≈ Title) | ~0.32k | the canned path generalizes at the same cost |
| v5: REST-only gh, guarded and resumable publish (non-src guard, PUBLISH_FAIL line) | 14 | tbd | tbd | prevents the iter 13 failure mode (+33k input) |

## Plan
1. **Iter 1 (baseline):** stock feature-loop skill from master, no lean scripts. The agent must poll CI to green and fix reds.
2. **Iters 2+:** start from `analysis/token-reduction`. Candidate techniques, introduced one or two at a time so their deltas can be attributed: (a) prebuilt change map, i.e. the table above; (b) Get-ItemContext for exact files; (c) a canned `Wait-PrChecks.ps1` (one blocking call: `gh pr checks --watch` with compact output, which turns many poll turns into 1); (d) canned failure-log extraction (`gh run view --log-failed` tail); (e) local targeted integration test (SQLite) before push; (f) lean skill with no exploration or board walk; (g) one canned "apply whole change" script, the template ceiling.
3. Record per-run `iter-NN.md` plus the ledger. Stop at 20, or earlier if gains plateau over 3 runs.

## Iterations
| Iter | Issue | PR | Input est. | Output est. | Calls | CI reds | Notes |
|---|---|---|---|---|---|---|---|
| 1 | #9977 | #9978 | ~1.68M+ | ~12.4k | 60 (+7 subagents) | 0 | baseline |
| 2 | #9979 | #9980 | ~13.4k | ~1.8k | 7 | 0 | lean v1 |
| 3 | #9981 | #9985 | ~97.4k | ~2.65k | 14 | 1 flake | v2 reference (flake) |
| 4 | #9982 | #9984 | ~20.5k | ~1.4k | 7 | 0 | ablate T3 |
| 5 | #9983 | #9986 | ~606k | ~3.1k | 42 | 0 | ablate T2 |
| 6 | #9987 | #9993 | ~1,183k | ~4.6k | 59 | 0 | ablate T1 |
| 7 | #9988 | #9992 | ~0.95k | ~0.49k | 2 | 0 | ablate T4 |
| 8 | #9989 | #9994 | ~693k | ~10.6k | 38 | 0 | ablate T5 (card moved to Functional Testing; needs reset) |
| 9 | #9990 | #9991 | ~0.86k | ~0.45k | 2 | 0 | v3 reference |
| 10 | #9995 | #10000 | ~4.5k | ~0.44k | 4 | 0 | v4 reference (backgrounded at cap) |
| 11 | #9996 | #9999 | ~0.63k | ~0.31k | 2 | 0 | v4 repeat |
| 12 | #9997 | #10001 | ~0.64k | ~0.32k | 2 | 0 | RoomNumber 1200 |
| 13 | #9998 | #10003 | ~33.6k | ~2.2k | 11 | 0 | v3 prompt; publish bug, 54-file PR |
| 14 | #10004 | | | | | | v5 reference (REST, guarded publish, call under the 600 s cap) |
| 15 | #10005 | | | | | | command-list prompt (output technique) |
| 16 | #10006 | | | | | | Title -Max 1200 (value generalization) |
| 17 | #10007 | | | | | | v5 repeat (variance) |
| 18 | #10008 | | | | | | hand edits with file map + `-SkipApply` (non-canned floor) |
| 19 | #10009 | | | | | | v5 repeat |
| 20 | #10010 | | | | | | v5 final repeat |
