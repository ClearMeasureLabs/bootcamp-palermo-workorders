## 1. State Command Implementation

- [x] 1.1 Create `CompleteToInProgressCommand.cs` in `src/Core/Model/StateCommands/` following the `StateCommandBase` pattern
- [x] 1.2 Register `CompleteToInProgressCommand` in `StateCommandList.GetAllStateCommands()`

## 2. Unit Tests

- [x] 2.1 Create `CompleteToInProgressCommandTests.cs` in `src/UnitTests/Core/Model/StateCommands/` with tests for begin/end status, verb tenses, authorization (assignee only), and CompletedDate clearing
- [x] 2.2 Update `StateCommandListTests.cs` to verify the new command is included in the list

## 3. Integration Tests

- [x] 3.1 Create `StateCommandHandlerForReopenTests.cs` in `src/IntegrationTests/DataAccess/Handlers/` to test the full handler pipeline for the Complete -> InProgress transition

## 4. UI Updates

- [x] 4.1 No UI code changes needed — the "Reopen" button appears automatically via `ValidCommands` dynamic rendering in `WorkOrderManage.razor`
- [x] 4.2 No wiring needed — the existing `HandleSubmit` + `StateCommandList.GetMatchingCommand` pattern handles the new command automatically

## 5. Acceptance Tests

- [x] 5.1 Updated `WorkOrderCompleteTests` to verify the "Reopen" button is visible to the assignee on a completed work order (previously expected read-only view)
- [x] 5.2 Updated `WorkOrderSpeechTests` to verify speak buttons remain visible on completed work orders with the Reopen feature

## 6. Verification

- [x] 6.1 All CI checks pass: Security Scan, Code Analysis, Integration Build (SQL container, SQLite, ARM SQLite, Windows LocalDB), Acceptance Tests, Acceptance Tests (ARM SQLite), Publish to GitHub Packages, Publish to Octopus Deploy, Publish Release Candidate
