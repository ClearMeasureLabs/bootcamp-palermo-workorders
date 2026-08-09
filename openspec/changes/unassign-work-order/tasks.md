## 1. State Command Implementation

- [ ] 1.1 Create `AssignedToDraftCommand.cs` in `src/Core/Model/StateCommands/` following the `StateCommandBase` pattern with begin status Assigned, end status Draft, verb "Unassign", and creator-only authorization
- [ ] 1.2 Implement `Execute` override to clear `Assignee` and `AssignedDate` before calling `base.Execute`
- [ ] 1.3 Register `AssignedToDraftCommand` in `StateCommandList.GetAllStateCommands` after `AssignedToCancelledCommand`

## 2. Unit Tests

- [ ] 2.1 Create `AssignedToDraftCommandTests.cs` in `src/UnitTests/Core/Model/StateCommands/` extending `StateCommandBaseTests`
- [ ] 2.2 Add test: `ShouldNotBeValidInWrongStatus` — verify `IsValid()` returns false when work order is not in Assigned status
- [ ] 2.3 Add test: `ShouldNotBeValidWithWrongEmployee` — verify `IsValid()` returns false when current user is not the creator
- [ ] 2.4 Add test: `ShouldBeValid` — verify `IsValid()` returns true when work order is Assigned and current user is the creator
- [ ] 2.5 Add test: `ShouldTransitionStateProperly` — verify `Execute` changes status to Draft, clears assignee, and clears assigned date
- [ ] 2.6 Update `StateCommandListTests` to account for the new command (expected count increases from 6 to 7)

## 3. Integration Tests

- [ ] 3.1 Create `StateCommandHandlerForUnassignTests.cs` in `src/IntegrationTests/DataAccess/Handlers/` following the pattern from `StateCommandHandlerForCancelTests.cs`
- [ ] 3.2 Add test: verify `AssignedToDraftCommand` persists the work order with Draft status, null assignee, and null assigned date

## 4. Architecture Documentation

- [ ] 4.1 Update `arch/arch-state-workorder.md` to include the `Assigned --> Draft : AssignedToDraftCommand` transition in the Mermaid state diagram

## 5. Verification

- [ ] 5.1 Run unit tests and confirm all pass including the new tests
- [ ] 5.2 Run integration tests and confirm all pass including the new tests
- [ ] 5.3 Verify the solution builds end-to-end with `dotnet build src/ChurchBulletin.sln`
