## 1. Core Implementation

- [x] 1.1 Create `CompleteToAssignedCommand.cs` in `src/Core/Model/StateCommands/`
- [x] 1.2 Register `CompleteToAssignedCommand` in `StateCommandList.GetAllStateCommands()`
- [x] 1.3 Update `WorkOrder.CanReassign()` to return true for Complete status

## 2. Unit Tests

- [x] 2.1 Create `CompleteToAssignedCommandTests.cs` in `src/UnitTests/Core/Model/StateCommands/`
- [x] 2.2 Update `StateCommandListTests.ShouldReturnAllStateCommandsInCorrectOrder` (count 6→7, add type assertion)

## 3. Integration Tests

- [x] 3.1 Create `StateCommandHandlerForReassignTests.cs` in `src/IntegrationTests/DataAccess/Handlers/`

## 4. Verification

- [x] 4.1 Build the solution and run all unit tests
- [x] 4.2 Run integration tests (if database is available)
- [x] 4.3 Fix acceptance tests for new creator behavior on completed work orders
- [x] 4.4 All CI checks passing (11/11 green)
