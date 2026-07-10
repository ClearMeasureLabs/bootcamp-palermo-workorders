## Why

Work order creators need a way to provide detailed instructions about how to perform the work described in a work order. Currently, the create/edit screen only has Title and Description fields. The Description field captures what the work order is about, but there is no dedicated field for step-by-step or detailed instructions on how to carry out the work. This leads to creators overloading the Description field or communicating instructions through other channels, which reduces clarity and traceability.

## What Changes

- New optional `Instructions` property on the `WorkOrder` entity, stored as `NVARCHAR(4000)` in the database
- New `InputTextArea` field on the Work Order create/edit screen, positioned below the Description field
- Database migration to add the `Instructions` column to the `dbo.WorkOrder` table
- EF Core mapping for the new property
- Domain model truncation logic matching the Description pattern (4000 character limit)
- Updates to the view model, code-behind, and MCP tools to support the new field

## Capabilities

### New Capabilities
- `work-order-instructions`: An optional Instructions field on the WorkOrder entity, persisted to the database, editable on the create/edit screen, and exposed through the MCP server tools

### Modified Capabilities
<!-- No existing spec-level behavior changes beyond adding the new field to existing screens and tools -->

## Impact

- **Domain model**: `WorkOrder` class in `src/Core/Model/WorkOrder.cs` gains an `Instructions` property with truncation logic
- **Database**: New migration script `028_AddInstructionsToWorkOrder.sql` adds the column
- **EF Core**: `WorkOrderMap.cs` updated with `HasMaxLength(4000)` mapping
- **UI**: `WorkOrderManage.razor` gains an Instructions textarea below Description; `WorkOrderManageModel` gains an `Instructions` property
- **Code-behind**: `WorkOrderManage.razor.cs` maps Instructions between model and view model, and copies it during submit
- **MCP Server**: `WorkOrderTools.cs` updated to accept and display Instructions
- **Tests**: Unit tests and integration tests updated to cover the new property
- **No breaking changes**: The field is optional with no `[Required]` constraint
