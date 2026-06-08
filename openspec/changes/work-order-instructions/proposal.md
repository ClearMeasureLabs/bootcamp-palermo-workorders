## Why

The work order create/manage screen currently captures a Title and Description, but there is no way for the work order creator to provide detailed instructions on how the work should be performed. Creators often need to communicate step-by-step procedures, safety precautions, or specific methods that go beyond a general description. Without an Instructions field, this information either gets crammed into the Description field (which has a different purpose — describing _what_ needs to be done) or is communicated out-of-band via email or verbal instructions, leading to information loss and miscommunication.

Issue: #775 — "Create work order instructions"

## What Changes

- New `Instructions` property on the `WorkOrder` domain entity — an optional `string` field with a maximum length of 4000 characters
- New database column `Instructions` on `dbo.WorkOrder` via a DbUp migration script (`028_AddInstructionsToWorkOrder.sql`)
- EF Core mapping updated in `WorkOrderMap` to configure the new column
- `WorkOrderManageModel` view model updated with an `Instructions` property
- `WorkOrderManage.razor` updated with an Instructions `InputTextArea` field placed below Description, with a megaphone/speak button following the existing pattern
- Code-behind `WorkOrderManage.razor.cs` updated to map Instructions between the view model and domain entity
- MCP server `WorkOrderTools` updated to include Instructions in work order details and accept it during creation
- LLM gateway `WorkOrderTool` updated to include Instructions in work order retrieval results
- Unit tests for the new property on `WorkOrder` and the UI rendering
- Integration tests verifying Instructions persists through a database round-trip
- Acceptance tests verifying the Instructions field works end-to-end in the browser

## Capabilities

### New Capabilities
- `domain-model`: `WorkOrder.Instructions` property for capturing detailed work instructions
- `database-migration`: Schema migration adding the `Instructions` column to `dbo.WorkOrder`
- `work-order-manage-ui`: Instructions text area on the work order create/manage screen with speech synthesis support

### Modified Capabilities
- `ef-core-mapping`: `WorkOrderMap` updated to map the new `Instructions` column
- `view-model`: `WorkOrderManageModel` updated with `Instructions` property
- `mcp-server-update`: MCP work order tools updated to include Instructions in responses and accept it during creation
- `unit-tests`: New tests in `WorkOrderTests` for the Instructions property; bUnit tests for UI rendering
- `integration-tests`: New test in `WorkOrderQueryHandlerTests` verifying Instructions persistence
- `acceptance-tests`: Updated `WorkOrderSaveDraftTests` to fill in and verify the Instructions field

## Impact

- **Domain (Core)**: One new property added to `WorkOrder` entity — no new project references, maintains Onion Architecture
- **Database**: New nullable `NVARCHAR(4000)` column on `dbo.WorkOrder` via script `028_AddInstructionsToWorkOrder.sql`
- **DataAccess**: `WorkOrderMap` updated with one additional property mapping
- **UI.Shared**: `WorkOrderManageModel` gains one property; `WorkOrderManage.razor` gains one form group; code-behind maps the property bidirectionally
- **McpServer**: `WorkOrderTools` updated to serialize/deserialize Instructions
- **LlmGateway**: `WorkOrderTool` updated to include Instructions in returned data
- **Tests**: New unit, integration, and acceptance tests — no existing tests should break
- **CI/CD**: No pipeline changes needed — the new migration script runs automatically via DbUp
- **Deployment**: Database migration is backward-compatible (nullable column addition)
