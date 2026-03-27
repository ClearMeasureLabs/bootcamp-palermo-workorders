## ADDED Requirements

### Requirement: Complete to InProgress state transition command
The system SHALL provide a `CompleteToInProgressCommand` state command that transitions a work order from `Complete` status to `InProgress` status.

#### Scenario: Assignee reopens a completed work order
- **GIVEN** a work order exists with status `Complete`
- **AND** the work order has an assignee
- **WHEN** the assignee executes the `CompleteToInProgressCommand`
- **THEN** the work order status SHALL change to `InProgress`
- **AND** the `CompletedDate` SHALL be cleared (set to `null`)

#### Scenario: Non-assignee cannot reopen a completed work order
- **GIVEN** a work order exists with status `Complete`
- **AND** the current user is NOT the assignee of the work order
- **WHEN** the user attempts to execute the `CompleteToInProgressCommand`
- **THEN** the command SHALL fail validation
- **AND** the work order status SHALL remain `Complete`

#### Scenario: Command cannot be executed on non-complete work order
- **GIVEN** a work order exists with a status other than `Complete` (e.g., Draft, Assigned, InProgress)
- **WHEN** a user attempts to execute the `CompleteToInProgressCommand`
- **THEN** the command SHALL fail validation
- **AND** the work order status SHALL remain unchanged

### Requirement: Reopen button in work order manage UI
The work order manage page SHALL display a "Reopen" button that allows the assignee to reopen a completed work order.

#### Scenario: Reopen button visible to assignee on completed work order
- **GIVEN** the current user is the assignee of a completed work order
- **WHEN** the user views the work order manage page
- **THEN** a "Reopen" button SHALL be visible

#### Scenario: Reopen button not visible to non-assignee on completed work order
- **GIVEN** the current user is NOT the assignee of a completed work order
- **WHEN** the user views the work order manage page
- **THEN** a "Reopen" button SHALL NOT be visible

#### Scenario: Clicking Reopen transitions work order to InProgress
- **GIVEN** the current user is the assignee of a completed work order
- **AND** the "Reopen" button is visible
- **WHEN** the user clicks the "Reopen" button
- **THEN** the work order status SHALL change to `InProgress`
- **AND** the page SHALL reflect the updated status

### Requirement: State command registration
The `CompleteToInProgressCommand` SHALL be registered in `StateCommandList.GetAllStateCommands()` so it is discoverable by the `StateCommandHandler`, the UI, and the MCP server.

#### Scenario: Command is included in the state command list
- **GIVEN** the `StateCommandList.GetAllStateCommands()` method is called
- **WHEN** the returned list is inspected
- **THEN** it SHALL contain an entry for `CompleteToInProgressCommand`

### Constraints
- The command class SHALL follow the `StateCommandBase` record pattern used by all existing state commands
- The command class SHALL be located at `src/Core/Model/StateCommands/CompleteToInProgressCommand.cs`
- The command's `Name` constant SHALL be `"Reopen"`
- The command's `TransitionVerbPastTense` SHALL return `"Reopened"`
- The command's `GetBeginStatus()` SHALL return `WorkOrderStatus.Complete`
- The command's `GetEndStatus()` SHALL return `WorkOrderStatus.InProgress`
- The command's `UserCanExecute()` SHALL return `true` only when the current user equals `WorkOrder.Assignee`
- The command's `Execute()` SHALL set `WorkOrder.CompletedDate` to `null` before calling `base.Execute(context)`
- Unit tests SHALL be located at `src/UnitTests/Core/Model/StateCommands/CompleteToInProgressCommandTests.cs`
- Integration tests SHALL be located at `src/IntegrationTests/DataAccess/Handlers/StateCommandHandlerForReopenTests.cs`
- All tests SHALL follow the project's NUnit 4.x + Shouldly conventions with AAA pattern and `Should`/`When` naming
