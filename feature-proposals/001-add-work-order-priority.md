## Why
Work orders currently have no way to indicate urgency. Adding a Priority field (Low, Medium, High, Critical) enables teams to triage and address the most important issues first. This supports better resource allocation and ensures critical facility issues receive immediate attention.

## What Changes
- Add `WorkOrderPriority` smart enum to `src/Core/Model/` with values: Low, Medium, High, Critical (following the `WorkOrderStatus` pattern)
- Add `Priority` property of type `WorkOrderPriority` to the `WorkOrder` domain model
- Update `DataContext` EF Core mapping to persist `Priority` as an integer column
- Add a new DbUp migration script to `src/Database/scripts/Update/` adding a `Priority` column (int, NOT NULL, default 0) to the `WorkOrder` table
- Update `WorkOrderManage` Blazor form to include a Priority dropdown selector
- Update `WorkOrderSearch` page to include a Priority filter dropdown
- Update `WorkOrderSearchQuery` to support filtering by priority
- Update MCP tools to expose priority in work order data

## Capabilities
### New Capabilities
- Users can set a priority level (Low, Medium, High, Critical) when creating or editing a work order
- Users can filter work orders by priority on the search page
- MCP tools return priority information for work orders

### Modified Capabilities
- WorkOrderManage form includes a new Priority dropdown field
- WorkOrderSearch results display the priority value and support filtering

## Impact
- **Core** — New `WorkOrderPriority` smart enum class; `WorkOrder` model gains `Priority` property
- **DataAccess** — EF Core mapping update for `Priority` column; value converter for smart enum; search handler updated for priority filter
- **UI.Shared** — `WorkOrderManage` form updated with dropdown; `WorkOrderSearch` page updated with filter and display column
- **Database** — New migration script adding `Priority` column to `WorkOrder` table
- **McpServer** — MCP tool responses updated to include priority

## Acceptance Criteria
### Unit Tests
- `WorkOrderPriority_FromCode_ShouldReturnCorrectEnum` — verify all four priority codes resolve to correct enum values
- `WorkOrderPriority_FromKey_ShouldReturnCorrectEnum` — verify key-based lookup works
- `WorkOrder_ShouldDefaultPriority_ToLow` — verify new work orders default to Low priority
- `WorkOrderManage_ShouldRenderPriorityDropdown` — bUnit test verifying dropdown appears with all options

### Integration Tests
- `WorkOrder_WithPriority_ShouldPersistAndRetrieve` — save a work order with High priority and verify it round-trips through the database
- `WorkOrderSearchQuery_FilterByPriority_ShouldReturnMatchingResults` — verify search filtering returns only work orders with the specified priority

### Acceptance Tests
- Navigate to create work order form, select "High" priority, save, and verify the priority is displayed on the work order detail page
- Navigate to work order search, filter by "Critical" priority, and verify only critical work orders appear in results
