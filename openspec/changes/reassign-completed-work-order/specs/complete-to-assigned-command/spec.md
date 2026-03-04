## ADDED Requirements

### Requirement: CompleteToAssignedCommand transitions a completed work order to Assigned status
The system SHALL provide a `CompleteToAssignedCommand` state command that transitions a work order from Complete status to Assigned status. The command verb SHALL be "Reassign" (present tense) / "Reassigned" (past tense).

#### Scenario: Valid reassignment by creator
- **WHEN** a work order is in Complete status
- **AND** the current user is the creator of the work order
- **THEN** the command `IsValid()` returns true

#### Scenario: Invalid reassignment — wrong status
- **WHEN** a work order is NOT in Complete status
- **AND** the current user is the creator of the work order
- **THEN** the command `IsValid()` returns false

#### Scenario: Invalid reassignment — wrong user
- **WHEN** a work order is in Complete status
- **AND** the current user is NOT the creator of the work order
- **THEN** the command `IsValid()` returns false

#### Scenario: Execute sets correct dates
- **WHEN** the command is executed
- **THEN** the work order status transitions to Assigned
- **AND** `AssignedDate` is set to the current date/time
- **AND** `CompletedDate` is set to null

### Requirement: CompleteToAssignedCommand is registered in StateCommandList
The system SHALL include `CompleteToAssignedCommand` in the `StateCommandList.GetAllStateCommands()` method, bringing the total state command count from 6 to 7.

#### Scenario: Command list includes CompleteToAssignedCommand
- **WHEN** `GetAllStateCommands()` is called
- **THEN** the returned array contains 7 commands
- **AND** one of the commands is of type `CompleteToAssignedCommand`

## MODIFIED Requirements

### Requirement: WorkOrder.CanReassign() includes Complete status
The `CanReassign()` method SHALL return true when the work order status is Draft OR Complete.

#### Scenario: CanReassign returns true for Complete status
- **WHEN** a work order is in Complete status
- **THEN** `CanReassign()` returns true

#### Scenario: CanReassign returns true for Draft status (unchanged)
- **WHEN** a work order is in Draft status
- **THEN** `CanReassign()` returns true

#### Scenario: CanReassign returns false for other statuses
- **WHEN** a work order is in Assigned, InProgress, or Cancelled status
- **THEN** `CanReassign()` returns false
