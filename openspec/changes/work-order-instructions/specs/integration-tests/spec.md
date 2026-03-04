## CHANGED Requirements

### Requirement: WorkOrderMappingTests verifies Instructions round-trip persistence
The existing integration tests in `src/IntegrationTests/DataAccess/Mappings/WorkOrderMappingTests.cs` SHALL be updated to include the `Instructions` field in work order creation and verification.

#### Scenario: ShouldMapWorkOrderBasicProperties includes Instructions
- **GIVEN** the existing test `ShouldMapWorkOrderBasicProperties` in `src/IntegrationTests/DataAccess/Mappings/WorkOrderMappingTests.cs`
- **WHEN** the test is updated
- **THEN** the `WorkOrder` initialization SHALL include `Instructions = "Check all light fixtures and replace any burnt out bulbs"`
- **AND** the rehydration assertion SHALL include `rehydratedWorkOrder.Instructions.ShouldBe("Check all light fixtures and replace any burnt out bulbs")`

#### Scenario: ShouldSaveWorkOrder includes Instructions
- **GIVEN** the existing test `ShouldSaveWorkOrder` in `src/IntegrationTests/DataAccess/Mappings/WorkOrderMappingTests.cs`
- **WHEN** the test is updated
- **THEN** the `WorkOrder` initialization SHALL include `Instructions = "baz"`
- **AND** the rehydration assertion SHALL include `rehydratedWorkOrder.Instructions.ShouldBe(order.Instructions)`

#### Scenario: ShouldMapWorkOrderWithNullInstructions
- **GIVEN** test method `ShouldMapWorkOrderWithNullInstructions` exists in `src/IntegrationTests/DataAccess/Mappings/WorkOrderMappingTests.cs`
- **AND** a `WorkOrder` is created without setting `Instructions` (relying on the default of `null`)
- **AND** the work order is saved to the database
- **WHEN** the work order is retrieved from the database
- **THEN** `rehydratedWorkOrder.Instructions.ShouldBeNull()` SHALL pass

### Requirement: StateCommandHandlerForSaveTests verifies Instructions persistence
The existing integration tests in `src/IntegrationTests/DataAccess/Handlers/StateCommandHandlerForSaveTests.cs` SHALL verify that the `Instructions` field is persisted through the `SaveDraftCommand` handler.

#### Scenario: Instructions persisted through SaveDraftCommand
- **GIVEN** the existing save draft integration test
- **WHEN** a `WorkOrder` with `Instructions` set is saved via `SaveDraftCommand`
- **THEN** the rehydrated work order SHALL have the same `Instructions` value

### Constraints
- Integration test changes SHALL be made to existing files, NOT new files
- Tests SHALL follow the existing patterns: `new DatabaseTests().Clean()` at start, `TestHost.GetRequiredService<DbContext>()` for setup, Shouldly assertions
- Tests SHALL use the same assertion style already present in the file (`ShouldBe`, `ShouldNotBeNull`, `ShouldBeNull`)
