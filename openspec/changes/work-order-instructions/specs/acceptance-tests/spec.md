## MODIFIED Requirements

### Requirement: Acceptance tests verify Instructions field end-to-end
The acceptance tests SHALL be updated to fill in and verify the Instructions field during work order creation and editing.

#### Scenario: CreateAndSaveNewWorkOrder helper fills in Instructions
- **GIVEN** the `CreateAndSaveNewWorkOrder()` method in `AcceptanceTestBase.cs`
- **WHEN** the method is updated for the Instructions feature
- **THEN** it SHALL call `Input(WorkOrderManage.Elements.Instructions, workOrder.Instructions)` to fill in the Instructions field
- **AND** the fake `WorkOrder` created in the method SHALL have `Instructions` set to a test value (e.g., using AutoBogus or a hardcoded test string)

#### Scenario: ShouldCreateNewWorkOrderAndVerifyOnSearchScreen includes Instructions
- **GIVEN** the existing test `ShouldCreateNewWorkOrderAndVerifyOnSearchScreen` in `WorkOrderSaveDraftTests.cs`
- **WHEN** the test is updated for the Instructions feature
- **THEN** after navigating to the work order manage screen, the test SHALL read the Instructions field value via `GetValue(WorkOrderManage.Elements.Instructions)`
- **AND** the test SHALL assert that the displayed Instructions matches the value set during creation
- **AND** the test SHALL assert that the rehydrated work order from the database has the correct Instructions value

#### Scenario: ShouldAssignEmployeeAndSave includes Instructions
- **GIVEN** the existing test `ShouldAssignEmployeeAndSave` in `WorkOrderSaveDraftTests.cs`
- **WHEN** the test is updated for the Instructions feature
- **THEN** the test SHALL fill in Instructions during the edit step using `Input(WorkOrderManage.Elements.Instructions, newInstructions)`
- **AND** the test SHALL verify the Instructions value persisted after save

### Constraints
- Acceptance tests SHALL use `data-testid` attributes via `WorkOrderManage.Elements.Instructions` for element location
- The `Input()` helper method SHALL be used for filling in the Instructions field (same pattern as Title, Description, RoomNumber)
- The `GetValue()` helper method SHALL be used for reading the Instructions field value
- Tests SHALL follow the existing Playwright test patterns in `AcceptanceTestBase.cs`
