## 1. Core Implementation

- [ ] 1.1 Create `CompleteToAssignedCommand.cs` in `src/Core/Model/StateCommands/`
- [ ] 1.2 Register `CompleteToAssignedCommand` in `StateCommandList.GetAllStateCommands()`
- [ ] 1.3 Update `WorkOrder.CanReassign()` to return true for Complete status

## 2. Unit Tests

- [ ] 2.1 Create `CompleteToAssignedCommandTests.cs` in `src/UnitTests/Core/Model/StateCommands/`
- [ ] 2.2 Update `StateCommandListTests.ShouldReturnAllStateCommandsInCorrectOrder` (count 6→7, add type assertion)

## 3. Integration Tests

- [ ] 3.1 Create `StateCommandHandlerForReassignTests.cs` in `src/IntegrationTests/DataAccess/Handlers/`

## 4. Verification

- [ ] 4.1 Build the solution and run all unit tests
- [ ] 4.2 Run integration tests (if database is available)
