## CHANGED Requirements

### Requirement: CreateAndSaveNewWorkOrder fills Instructions field
The `CreateAndSaveNewWorkOrder()` helper method in `src/AcceptanceTests/AcceptanceTestBase.cs` SHALL be updated to fill in the Instructions field when creating a new work order.

#### Scenario: Instructions is filled during work order creation
- **WHEN** the `CreateAndSaveNewWorkOrder()` method is examined
- **THEN** it SHALL generate a test value for Instructions from the `Faker<WorkOrder>()` (e.g., `var testInstructions = order.Instructions;`)
- **AND** it SHALL call `await Input(nameof(WorkOrderManage.Elements.Instructions), testInstructions);` after filling the Description field and before filling the RoomNumber field

### Requirement: Acceptance test verifies Instructions field on work order edit page
The existing acceptance tests in `src/AcceptanceTests/WorkOrders/WorkOrderSaveDraftTests.cs` SHALL verify that the Instructions field is visible, editable, and persists its value after saving.

#### Scenario: ShouldDisplayInstructionsFieldOnNewWorkOrder
- **GIVEN** an acceptance test navigates to the new work order page
- **WHEN** the page is rendered
- **THEN** `Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions))` SHALL be visible and editable

#### Scenario: ShouldPersistInstructionsAfterSave
- **GIVEN** a work order is created with Instructions filled in via `CreateAndSaveNewWorkOrder()`
- **AND** the user navigates to the saved work order via `ClickWorkOrderNumberFromSearchPage(order)`
- **WHEN** the Instructions field value is read
- **THEN** the Instructions field SHALL contain the value that was entered (uppercased if `SaveDraftCommand` transforms it, or the original value if not)

### Constraints
- Changes to `CreateAndSaveNewWorkOrder()` SHALL be in the existing `src/AcceptanceTests/AcceptanceTestBase.cs` file
- Acceptance test changes SHALL be in existing files where appropriate
- Tests SHALL use `Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions))` for element location
- Tests SHALL use the `Input()` helper method from `AcceptanceTestBase` for filling the Instructions field
- Tests SHALL follow the existing patterns: `[Test, Retry(2)]`, `LoginAsCurrentUser()`, Shouldly/Playwright assertions
