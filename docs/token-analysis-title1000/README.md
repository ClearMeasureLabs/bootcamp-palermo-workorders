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
| v2 fix: scripts copied to temp before the branch switch | 3 | tbd | tbd | expect ~4 fewer calls |
| per-technique ablations | 4-8 | see ablation-plan.md | | |

## Plan
1. **Iter 1 (baseline):** stock feature-loop skill from master, no lean scripts. The agent must poll CI to green and fix reds.
2. **Iters 2+:** start from `analysis/token-reduction`. Candidate techniques, introduced one or two at a time so their deltas can be attributed: (a) prebuilt change map, i.e. the table above; (b) Get-ItemContext for exact files; (c) a canned `Wait-PrChecks.ps1` (one blocking call: `gh pr checks --watch` with compact output, which turns many poll turns into 1); (d) canned failure-log extraction (`gh run view --log-failed` tail); (e) local targeted integration test (SQLite) before push; (f) lean skill with no exploration or board walk; (g) one canned "apply whole change" script, the template ceiling.
3. Record per-run `iter-NN.md` plus the ledger. Stop at 20, or earlier if gains plateau over 3 runs.

## Iterations
| Iter | Issue | PR | Input est. | Output est. | Calls | CI reds | Notes |
|---|---|---|---|---|---|---|---|
| 1 | #9977 | #9978 | ~1.68M+ | ~12.4k | 60 (+7 subagents) | 0 | baseline |
| 2 | #9979 | #9980 | ~13.4k | ~1.8k | 7 | 0 | lean v1 |
| 3 | #9981 | | | | | | v2 reference |
| 4 | #9982 | | | | | | ablate T3 (CI wait) |
| 5 | #9983 | | | | | | ablate T2 (canned change) |
