## CHANGED Requirements

### Requirement: MCP CreateWorkOrder tool accepts an instructions parameter
The `CreateWorkOrder()` method in `src/McpServer/Tools/WorkOrderTools.cs` SHALL accept an optional `instructions` parameter of type `string?` and set it on the `WorkOrder` entity before saving.

#### Scenario: CreateWorkOrder accepts instructions parameter
- **WHEN** the `CreateWorkOrder()` method signature is examined
- **THEN** it SHALL include a parameter `[Description("Optional detailed instructions for how to perform the work order")] string? instructions = null`
- **AND** the parameter SHALL appear after the `roomNumber` parameter

#### Scenario: CreateWorkOrder sets instructions on work order
- **WHEN** `CreateWorkOrder()` is called with `instructions = "Step 1: Check wiring. Step 2: Replace outlet."`
- **THEN** the created `WorkOrder` object SHALL have `Instructions = "Step 1: Check wiring. Step 2: Replace outlet."`

### Requirement: MCP FormatWorkOrderDetail includes Instructions
The `FormatWorkOrderDetail()` method in `src/McpServer/Tools/WorkOrderTools.cs` SHALL include the `Instructions` property in the serialized output.

#### Scenario: FormatWorkOrderDetail includes Instructions field
- **WHEN** the `FormatWorkOrderDetail()` method is examined
- **THEN** the anonymous object SHALL include `wo.Instructions`
- **AND** it SHALL appear after `wo.Description` and before the `Status` property

### Requirement: LLM gateway tool description mentions Instructions
The `GetWorkOrderByNumber()` method's `[Description]` attribute in `src/LlmGateway/WorkOrderTool.cs` SHALL be updated to mention the Instructions field.

#### Scenario: Tool description includes instructions
- **WHEN** the `[Description]` attribute on `GetWorkOrderByNumber()` is examined
- **THEN** it SHALL mention "instructions" in the list of returned fields (e.g., "title, description, instructions, room number, status, ...")

### Constraints
- The `instructions` parameter on `CreateWorkOrder()` SHALL be optional with a default of `null`
- Changes SHALL be made to existing files: `src/McpServer/Tools/WorkOrderTools.cs` and `src/LlmGateway/WorkOrderTool.cs`
- The MCP tool description for `create-work-order` SHALL be updated to mention the new instructions parameter
