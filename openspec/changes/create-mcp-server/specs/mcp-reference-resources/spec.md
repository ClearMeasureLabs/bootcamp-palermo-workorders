## ADDED Requirements

### Requirement: Work order statuses resource
The system SHALL expose an MCP resource at URI `churchbulletin://reference/work-order-statuses` that returns all valid work order statuses.

#### Scenario: Read work order statuses
- **WHEN** an MCP client reads the `churchbulletin://reference/work-order-statuses` resource
- **THEN** all statuses are returned including their code, key, and friendly name (Draft, Assigned, InProgress, Complete)

### Requirement: Roles resource
The system SHALL expose an MCP resource at URI `churchbulletin://reference/roles` that returns all defined roles.

#### Scenario: Read roles
- **WHEN** an MCP client reads the `churchbulletin://reference/roles` resource
- **THEN** all roles are returned including their name and permissions (canCreateWorkOrder, canFulfillWorkOrder)

### Requirement: Valid status transitions resource
The system SHALL expose an MCP resource at URI `churchbulletin://reference/status-transitions` that returns the valid state transitions for work orders.

#### Scenario: Read valid transitions
- **WHEN** an MCP client reads the `churchbulletin://reference/status-transitions` resource
- **THEN** a mapping of each status to its valid target statuses is returned (e.g., Draft can transition to Assigned; Assigned can transition to InProgress)
