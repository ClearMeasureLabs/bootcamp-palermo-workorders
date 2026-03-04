## MODIFIED Requirements

### Requirement: MCP work order tools include Instructions
The MCP server's `WorkOrderTools` in `src/McpServer/Tools/WorkOrderTools.cs` SHALL be updated to include the `Instructions` field in work order responses and accept it during creation.

#### Scenario: FormatWorkOrderDetail includes Instructions
- **WHEN** the `FormatWorkOrderDetail` method formats a work order for MCP tool response
- **THEN** the returned object SHALL include an `Instructions` property containing the work order's Instructions value

#### Scenario: FormatWorkOrderSummary includes Instructions
- **WHEN** the `FormatWorkOrderSummary` method formats a work order for list responses
- **THEN** the returned object SHALL include an `Instructions` property (truncated if necessary, or the full value)

#### Scenario: create-work-order accepts optional instructions parameter
- **WHEN** the `create-work-order` MCP tool is invoked with a `title`, `description`, `creatorUsername`, and optional `instructions` parameter
- **THEN** the created work order SHALL have `Instructions` set to the provided value
- **AND** the `instructions` parameter SHALL be optional (work orders can be created without it)

#### Scenario: create-work-order without instructions
- **WHEN** the `create-work-order` MCP tool is invoked without the `instructions` parameter
- **THEN** the created work order SHALL have `Instructions` default to `""` (domain default)

### Requirement: LLM gateway work order tool includes Instructions
The `WorkOrderTool` in `src/LlmGateway/WorkOrderTool.cs` SHALL include Instructions in the data returned by `GetWorkOrderByNumber`.

#### Scenario: GetWorkOrderByNumber returns Instructions
- **WHEN** `GetWorkOrderByNumber` retrieves a work order
- **THEN** the returned string representation SHALL include the work order's Instructions value alongside Title, Description, and other fields

### Constraints
- The `instructions` parameter in `create-work-order` SHALL be marked with `[Description("Optional detailed instructions for how to perform the work")]` for AI client discoverability
- The `FormatWorkOrderDetail` method SHALL place Instructions after Description in the serialized object for logical grouping
- No changes to the MCP tool names or required parameters — Instructions is purely additive
