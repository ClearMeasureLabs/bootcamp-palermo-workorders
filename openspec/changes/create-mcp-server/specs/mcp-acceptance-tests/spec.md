## ADDED Requirements

### Requirement: MCP server acceptance test infrastructure
The system SHALL include acceptance tests in `src/AcceptanceTests/McpServer/` that start the MCP server as a child process using `StdioClientTransport`, connect an `McpClient`, wire the discovered tools into an `IChatClient` (Azure OpenAI), and validate end-to-end behavior through LLM prompts.

#### Scenario: MCP server starts and tools are discoverable
- **GIVEN** the McpServer project is built
- **WHEN** an `McpClient` connects via `StdioClientTransport` launching `dotnet run --project src/McpServer`
- **THEN** `ListToolsAsync()` returns at least 7 tools: `list-work-requests`, `get-work-request`, `create-work-request`, `execute-work-request-command`, `update-work-request-description`, `list-employees`, `get-employee`

#### Scenario: MCP server fixture manages process lifecycle
- **GIVEN** an NUnit `[SetUpFixture]` class `McpServerFixture` in the AcceptanceTests project
- **WHEN** tests in the `McpServer` namespace execute
- **THEN** the fixture starts the MCP server process once before all tests
- **AND** disposes the `McpClient` and kills the server process after all tests complete

### Requirement: LLM-driven work request query via MCP tools
The system SHALL accept a natural-language prompt asking about work requests, route the request through the LLM with MCP tools registered, and return a response containing data from the database.

#### Scenario: LLM lists work requests using MCP tool
- **GIVEN** the database contains seeded work requests
- **AND** an `IChatClient` is configured with MCP tools from the running MCP server
- **WHEN** the prompt "List all work requests in the system" is sent to the LLM
- **THEN** the LLM invokes the `list-work-requests` MCP tool
- **AND** the response contains work request numbers present in the database

#### Scenario: LLM retrieves a specific work request by number
- **GIVEN** a work request with a known number exists in the database
- **AND** an `IChatClient` is configured with MCP tools
- **WHEN** the prompt "Get the details of work request {number}" is sent to the LLM
- **THEN** the LLM invokes the `get-work-request` MCP tool
- **AND** the response contains the work request's title and status

### Requirement: LLM-driven work request creation via MCP tools
The system SHALL accept a natural-language prompt to create a work request, route it through the LLM with MCP tools, and persist a new work request in the database.

#### Scenario: LLM creates a work request using MCP tool
- **GIVEN** an employee with username `{username}` exists in the database
- **AND** an `IChatClient` is configured with MCP tools
- **WHEN** the prompt "Create a new work request titled 'Fix leaking roof' with description 'The roof in room 101 is leaking' by user {username}" is sent to the LLM
- **THEN** the LLM invokes the `create-work-request` MCP tool
- **AND** the response confirms the work request was created
- **AND** querying the database via `IBus` finds the new work request with status Draft

### Requirement: LLM-driven employee query via MCP tools
The system SHALL accept a natural-language prompt asking about employees and return employee data retrieved through MCP tools.

#### Scenario: LLM lists employees using MCP tool
- **GIVEN** the database contains seeded employees
- **AND** an `IChatClient` is configured with MCP tools
- **WHEN** the prompt "List all employees" is sent to the LLM
- **THEN** the LLM invokes the `list-employees` MCP tool
- **AND** the response contains employee names from the database

### Requirement: Tests are marked Explicit and tolerate LLM unavailability
Acceptance tests that require a running LLM SHALL be marked with `[Explicit]` so they do not run in standard CI pipelines. Tests SHALL handle LLM connection failures gracefully with `Assert.Inconclusive` rather than hard failures.

#### Scenario: Test skips when LLM is unavailable
- **GIVEN** no Azure OpenAI key is configured
- **WHEN** the MCP acceptance test attempts to connect the `IChatClient`
- **THEN** the test reports `Assert.Inconclusive("LLM not available")`
