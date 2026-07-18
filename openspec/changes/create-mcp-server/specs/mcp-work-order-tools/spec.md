## ADDED Requirements

### Requirement: List work requests tool
The system SHALL expose an MCP tool named `list-work-requests` that returns all work requests matching optional filter criteria.

#### Scenario: List all work requests
- **WHEN** the `list-work-requests` tool is invoked with no filters
- **THEN** all work requests are returned with their number, title, status, creator, and assignee

#### Scenario: Filter work requests by status
- **WHEN** the `list-work-requests` tool is invoked with a status filter (e.g., "Assigned")
- **THEN** only work requests matching that status are returned

### Requirement: Get work request by number tool
The system SHALL expose an MCP tool named `get-work-request` that retrieves a single work request by its number.

#### Scenario: Work request exists
- **WHEN** the `get-work-request` tool is invoked with a valid work request number
- **THEN** the full work request details are returned including number, title, description, status, room number, creator, assignee, and dates

#### Scenario: Work request does not exist
- **WHEN** the `get-work-request` tool is invoked with a number that does not match any work request
- **THEN** a message indicating no work request was found is returned

### Requirement: Create work request tool
The system SHALL expose an MCP tool named `create-work-request` that creates a new draft work request via the `SaveDraftCommand`.

#### Scenario: Valid draft creation
- **WHEN** the `create-work-request` tool is invoked with a title, description, and creator username
- **THEN** a new work request is created in Draft status
- **AND** the created work request details are returned

#### Scenario: Creator not found
- **WHEN** the `create-work-request` tool is invoked with a username that does not match any employee
- **THEN** an error message is returned indicating the employee was not found

### Requirement: Execute state command tool
The system SHALL expose an MCP tool named `execute-work-request-command` that executes a named state command (e.g., `DraftToAssignedCommand`, `AssignedToInProgressCommand`, `InProgressToCompleteCommand`) against a work request. Each `IStateCommand` implementation validates its own preconditions via `IsValid()` and defines the valid begin/end statuses.

#### Scenario: Valid command execution
- **WHEN** the `execute-work-request-command` tool is invoked with a work request number and command name (e.g., "AssignedToInProgressCommand")
- **AND** the command's preconditions are met (work request is in the correct begin status)
- **THEN** the state command executes and the work request transitions to the end status
- **AND** the updated work request details are returned

#### Scenario: Command preconditions not met
- **WHEN** the `execute-work-request-command` tool is invoked with a command whose preconditions are not satisfied (e.g., work request is not in the required begin status)
- **THEN** an error message is returned describing why the command cannot be executed

#### Scenario: Unknown command name
- **WHEN** the `execute-work-request-command` tool is invoked with a command name that does not match any registered `IStateCommand`
- **THEN** an error message listing the available command names is returned
