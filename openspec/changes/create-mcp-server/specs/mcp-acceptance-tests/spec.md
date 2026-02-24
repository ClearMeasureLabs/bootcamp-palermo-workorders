## ADDED Requirements

### Requirement: MCP server acceptance test infrastructure
The system SHALL include acceptance tests in `src/AcceptanceTests/McpServer/` that start the MCP server as a child process using `StdioClientTransport`, connect an `McpClient`, wire the discovered tools into an `IChatClient` (Ollama or Azure OpenAI), and validate end-to-end behavior through LLM prompts.

#### Scenario: MCP server starts and tools are discoverable
- **GIVEN** the McpServer project is built
- **WHEN** an `McpClient` connects via `StdioClientTransport` launching `dotnet run --project src/McpServer`
- **THEN** `ListToolsAsync()` returns at least 7 tools: `list-work-orders`, `get-work-order`, `create-work-order`, `execute-work-order-command`, `update-work-order-description`, `list-employees`, `get-employee`

#### Scenario: MCP server fixture manages process lifecycle
- **GIVEN** an NUnit `[SetUpFixture]` class `McpServerFixture` in the AcceptanceTests project
- **WHEN** tests in the `McpServer` namespace execute
- **THEN** the fixture starts the MCP server process once before all tests
- **AND** disposes the `McpClient` and kills the server process after all tests complete

### Requirement: LLM-driven work order query via MCP tools
The system SHALL accept a natural-language prompt asking about work orders, route the request through the LLM with MCP tools registered, and return a response containing data from the database.

#### Scenario: LLM lists work orders using MCP tool
- **GIVEN** the database contains seeded work orders
- **AND** an `IChatClient` is configured with MCP tools from the running MCP server
- **WHEN** the prompt "List all work orders in the system" is sent to the LLM
- **THEN** the LLM invokes the `list-work-orders` MCP tool
- **AND** the response contains work order numbers present in the database

#### Scenario: LLM retrieves a specific work order by number
- **GIVEN** a work order with a known number exists in the database
- **AND** an `IChatClient` is configured with MCP tools
- **WHEN** the prompt "Get the details of work order {number}" is sent to the LLM
- **THEN** the LLM invokes the `get-work-order` MCP tool
- **AND** the response contains the work order's title and status

### Requirement: LLM-driven work order creation via MCP tools
The system SHALL accept a natural-language prompt to create a work order, route it through the LLM with MCP tools, and persist a new work order in the database.

#### Scenario: LLM creates a work order using MCP tool
- **GIVEN** an employee with username `{username}` exists in the database
- **AND** an `IChatClient` is configured with MCP tools
- **WHEN** the prompt "Create a new work order titled 'Fix leaking roof' with description 'The roof in room 101 is leaking' by user {username}" is sent to the LLM
- **THEN** the LLM invokes the `create-work-order` MCP tool
- **AND** the response confirms the work order was created
- **AND** querying the database via `IBus` finds the new work order with status Draft

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
- **GIVEN** no Ollama instance is running locally
- **AND** no Azure OpenAI key is configured
- **WHEN** the MCP acceptance test attempts to connect the `IChatClient`
- **THEN** the test reports `Assert.Inconclusive("LLM not available")`
