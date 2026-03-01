## ADDED Requirements

### Requirement: List work orders tool
The system SHALL expose an MCP tool named `list-work-orders` that returns all work orders matching optional filter criteria.

#### Scenario: List all work orders
- **WHEN** the `list-work-orders` tool is invoked with no filters
- **THEN** all work orders are returned with their number, title, status, creator, and assignee

#### Scenario: Filter work orders by status
- **WHEN** the `list-work-orders` tool is invoked with a status filter (e.g., "Assigned")
- **THEN** only work orders matching that status are returned

### Requirement: Get work order by number tool
The system SHALL expose an MCP tool named `get-work-order` that retrieves a single work order by its number.

#### Scenario: Work order exists
- **WHEN** the `get-work-order` tool is invoked with a valid work order number
- **THEN** the full work order details are returned including number, title, description, status, room number, creator, assignee, and dates

#### Scenario: Work order does not exist
- **WHEN** the `get-work-order` tool is invoked with a number that does not match any work order
- **THEN** a message indicating no work order was found is returned

### Requirement: Create work order tool
The system SHALL expose an MCP tool named `create-work-order` that creates a new draft work order via the `SaveDraftCommand`.

#### Scenario: Valid draft creation
- **WHEN** the `create-work-order` tool is invoked with a title, description, and creator username
- **THEN** a new work order is created in Draft status
- **AND** the created work order details are returned

#### Scenario: Creator not found
- **WHEN** the `create-work-order` tool is invoked with a username that does not match any employee
- **THEN** an error message is returned indicating the employee was not found

### Requirement: Execute state command tool
The system SHALL expose an MCP tool named `execute-work-order-command` that executes a named state command (e.g., `DraftToAssignedCommand`, `AssignedToInProgressCommand`, `InProgressToCompleteCommand`, `InProgressToAssigned`) against a work order. Each `IStateCommand` implementation validates its own preconditions via `IsValid()` and defines the valid begin/end statuses.

#### Scenario: Valid command execution
- **WHEN** the `execute-work-order-command` tool is invoked with a work order number and command name (e.g., "AssignedToInProgressCommand")
- **AND** the command's preconditions are met (work order is in the correct begin status)
- **THEN** the state command executes and the work order transitions to the end status
- **AND** the updated work order details are returned

#### Scenario: Command preconditions not met
- **WHEN** the `execute-work-order-command` tool is invoked with a command whose preconditions are not satisfied (e.g., work order is not in the required begin status)
- **THEN** an error message is returned describing why the command cannot be executed

#### Scenario: Unknown command name
- **WHEN** the `execute-work-order-command` tool is invoked with a command name that does not match any registered `IStateCommand`
- **THEN** an error message listing the available command names is returned

### Requirement: Update work order description tool
The system SHALL expose an MCP tool named `update-work-order-description` that updates the description of an existing work order via the `UpdateDescriptionCommand`.

#### Scenario: Successful description update
- **WHEN** the `update-work-order-description` tool is invoked with a work order number and new description
- **THEN** the work order description is updated
- **AND** the updated work order details are returned

#### Scenario: Work order not found for description update
- **WHEN** the `update-work-order-description` tool is invoked with a number that does not match any work order
- **THEN** an error message is returned indicating the work order was not found
