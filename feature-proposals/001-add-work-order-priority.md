## Why
Work requests currently have no way to indicate urgency. Adding a Priority field (Low, Medium, High, Critical) enables teams to triage and address the most important issues first. This supports better resource allocation and ensures critical facility issues receive immediate attention.

## What Changes
- Add `WorkRequestPriority` smart enum to `src/Core/Model/` with values: Low, Medium, High, Critical (following the `WorkRequestStatus` pattern)
- Add `Priority` property of type `WorkRequestPriority` to the `WorkRequest` domain model
- Update `DataContext` EF Core mapping to persist `Priority` as an integer column
- Add a new DbUp migration script to `src/Database/scripts/Update/` adding a `Priority` column (int, NOT NULL, default 0) to the `WorkRequest` table
- Update `WorkRequestManage` Blazor form to include a Priority dropdown selector
- Update `WorkRequestSearch` page to include a Priority filter dropdown
- Update `WorkRequestSearchQuery` to support filtering by priority
- Update MCP tools to expose priority in work request data

## Capabilities
### New Capabilities
- Users can set a priority level (Low, Medium, High, Critical) when creating or editing a work request
- Users can filter work requests by priority on the search page
- MCP tools return priority information for work requests

### Modified Capabilities
- WorkRequestManage form includes a new Priority dropdown field
- WorkRequestSearch results display the priority value and support filtering

## Impact
- **Core** — New `WorkRequestPriority` smart enum class; `WorkRequest` model gains `Priority` property
- **DataAccess** — EF Core mapping update for `Priority` column; value converter for smart enum; search handler updated for priority filter
- **UI.Shared** — `WorkRequestManage` form updated with dropdown; `WorkRequestSearch` page updated with filter and display column
- **Database** — New migration script adding `Priority` column to `WorkRequest` table
- **McpServer** — MCP tool responses updated to include priority

## Acceptance Criteria
### Unit Tests
- `WorkRequestPriority_FromCode_ShouldReturnCorrectEnum` — verify all four priority codes resolve to correct enum values
- `WorkRequestPriority_FromKey_ShouldReturnCorrectEnum` — verify key-based lookup works
- `WorkRequest_ShouldDefaultPriority_ToLow` — verify new work requests default to Low priority
- `WorkRequestManage_ShouldRenderPriorityDropdown` — bUnit test verifying dropdown appears with all options

### Integration Tests
- `WorkRequest_WithPriority_ShouldPersistAndRetrieve` — save a work request with High priority and verify it round-trips through the database
- `WorkRequestSearchQuery_FilterByPriority_ShouldReturnMatchingResults` — verify search filtering returns only work requests with the specified priority

### Acceptance Tests
- Navigate to create work request form, select "High" priority, save, and verify the priority is displayed on the work request detail page
- Navigate to work request search, filter by "Critical" priority, and verify only critical work requests appear in results
