## ADDED Requirements

### Requirement: AssignedToDraftCommand transitions work order from Assigned to Draft
The system SHALL provide an `AssignedToDraftCommand` state command that transitions a work order from Assigned status back to Draft status. The command SHALL follow the `StateCommandBase` pattern and be registered in `StateCommandList`.

#### Scenario: Valid unassign by creator
- **WHEN** a work order is in Assigned status
- **AND** the creator of the work order executes the `AssignedToDraftCommand`
- **THEN** the work order status SHALL change to Draft
- **AND** the work order's assignee SHALL be cleared to null
- **AND** the work order's assigned date SHALL be cleared to null

#### Scenario: Command is invalid when work order is not in Assigned status
- **WHEN** a work order is in any status other than Assigned (e.g., Draft, InProgress, Complete, Cancelled)
- **AND** a user attempts to execute the `AssignedToDraftCommand`
- **THEN** the command SHALL report as invalid via `IsValid()` returning false

#### Scenario: Command is invalid when executed by non-creator
- **WHEN** a work order is in Assigned status
- **AND** a user who is not the creator of the work order attempts to execute the `AssignedToDraftCommand`
- **THEN** the command SHALL report as invalid via `IsValid()` returning false

#### Scenario: Command is valid only for the creator
- **WHEN** a work order is in Assigned status
- **AND** the current user is the creator of the work order
- **THEN** the command SHALL report as valid via `IsValid()` returning true

### Requirement: AssignedToDraftCommand uses "Unassign" as the transition verb
The system SHALL use "Unassign" as the `TransitionVerbPresentTense` for the `AssignedToDraftCommand`. This verb SHALL be used for command matching and UI display.

#### Scenario: Command matches by verb name
- **WHEN** `StateCommandList.GetMatchingCommand` is called with the name "Unassign"
- **AND** the work order is in Assigned status
- **AND** the current user is the creator
- **THEN** the `AssignedToDraftCommand` SHALL be returned

### Requirement: AssignedToDraftCommand is registered in StateCommandList
The system SHALL register `AssignedToDraftCommand` in the `StateCommandList.GetAllStateCommands` method so it is discoverable and available for validation and execution.

#### Scenario: Command appears in all state commands
- **WHEN** `StateCommandList.GetAllStateCommands` is called
- **THEN** the returned list SHALL include an instance of `AssignedToDraftCommand`

#### Scenario: Command appears in valid commands for creator of assigned work order
- **WHEN** `StateCommandList.GetValidStateCommands` is called with an Assigned work order and the creator as current user
- **THEN** the returned list SHALL include `AssignedToDraftCommand`

### Requirement: AssignedToDraftCommand persists state change
The system SHALL persist the work order state change when the `AssignedToDraftCommand` is executed through the `StateCommandHandler`. The persisted work order SHALL reflect Draft status with no assignee and no assigned date.

#### Scenario: State change is persisted to the database
- **WHEN** the `AssignedToDraftCommand` is executed via the `StateCommandHandler`
- **THEN** the work order SHALL be saved to the database with Draft status
- **AND** the assignee SHALL be null in the persisted record
- **AND** the assigned date SHALL be null in the persisted record
