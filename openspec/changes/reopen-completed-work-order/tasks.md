## 1. State Command Implementation

- [ ] 1.1 Create `CompleteToInProgressCommand.cs` in `src/Core/Model/StateCommands/` following the `StateCommandBase` pattern
- [ ] 1.2 Register `CompleteToInProgressCommand` in `StateCommandList.GetAllStateCommands()`

## 2. Unit Tests

- [ ] 2.1 Create `CompleteToInProgressCommandTests.cs` in `src/UnitTests/Core/Model/StateCommands/` with tests for begin/end status, verb tenses, authorization (assignee only), and CompletedDate clearing
- [ ] 2.2 Update `StateCommandListTests.cs` to verify the new command is included in the list

## 3. Integration Tests

- [ ] 3.1 Create `StateCommandHandlerForReopenTests.cs` in `src/IntegrationTests/DataAccess/Handlers/` to test the full handler pipeline for the Complete -> InProgress transition

## 4. UI Updates

- [ ] 4.1 Add a "Reopen" button to the work order manage page, visible when the work order is Complete and the current user is the assignee
- [ ] 4.2 Wire the button to invoke `CompleteToInProgressCommand` via `IBus`

## 5. Acceptance Tests

- [ ] 5.1 Add acceptance test verifying the "Reopen" button is visible to the assignee on a completed work order
- [ ] 5.2 Add acceptance test verifying the "Reopen" button transitions the work order back to InProgress

## 6. Verification

- [ ] 6.1 Run full private build (`PrivateBuild.ps1`) and verify all tests pass
