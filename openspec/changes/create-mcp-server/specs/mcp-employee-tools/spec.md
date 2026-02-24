## ADDED Requirements

### Requirement: List employees tool
The system SHALL expose an MCP tool named `list-employees` that returns all employees in the system.

#### Scenario: Employees exist
- **WHEN** the `list-employees` tool is invoked
- **THEN** all employees are returned with their username, first name, last name, email address, and roles

#### Scenario: No employees exist
- **WHEN** the `list-employees` tool is invoked and the database contains no employees
- **THEN** an empty list is returned

### Requirement: Get employee by username tool
The system SHALL expose an MCP tool named `get-employee` that retrieves a single employee by username.

#### Scenario: Employee exists
- **WHEN** the `get-employee` tool is invoked with a valid username
- **THEN** the employee details are returned including username, first name, last name, email address, and roles

#### Scenario: Employee not found
- **WHEN** the `get-employee` tool is invoked with a username that does not match any employee
- **THEN** a message indicating no employee was found is returned
