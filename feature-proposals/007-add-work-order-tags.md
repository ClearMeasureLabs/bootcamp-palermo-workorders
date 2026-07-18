## Why
Flexible tagging allows work orders to be grouped by ad-hoc criteria beyond fixed categories, supporting cross-cutting organization like "budget-approved", "safety-related", or "recurring". Tags provide a user-driven classification system that adapts to evolving organizational needs.

## What Changes
- Add `Tag` entity to `src/Core/Model/` with properties: Id (Guid), Name (string, unique)
- Add `WorkOrderTag` join entity to `src/Core/Model/` with properties: WorkOrderId (Guid), TagId (Guid)
- Add navigation property `Tags` (ICollection<Tag>) to `WorkOrder` domain model via many-to-many relationship
- Add `TagsQuery` to `src/Core/Queries/` to retrieve all available tags
- Add `AddTagToWorkOrderCommand` and `RemoveTagFromWorkOrderCommand` to `src/Core/Model/StateCommands/`
- Add EF Core mappings for `Tag` and `WorkOrderTag` join table in DataAccess
- Add handlers for tag commands and queries in `src/DataAccess/Handlers/`
- Add two new DbUp migration scripts: one for the `Tag` table and one for the `WorkOrderTag` join table
- Add a tag management component on the `WorkOrderManage` page with typeahead/autocomplete for existing tags
- Add a tag filter to `WorkOrderSearch` page

## Capabilities
### New Capabilities
- Users can add and remove tags on any work order
- Users can create new tags inline when adding a tag that does not exist
- Users can filter work orders by tag on the search page
- Tags are displayed as badges on work order search results

### Modified Capabilities
- WorkOrderManage page includes a new tags section with add/remove functionality
- WorkOrderSearch results display tags and support tag-based filtering

## Impact
- **Core** — New `Tag` entity; new `WorkOrderTag` join entity; new commands and query
- **DataAccess** — EF Core mappings for `Tag` and `WorkOrderTag` tables; new MediatR handlers for tag operations
- **UI.Shared** — Tag management component on `WorkOrderManage`; tag filter on `WorkOrderSearch`
- **Database** — Two new migration scripts creating `Tag` and `WorkOrderTag` tables

## Acceptance Criteria
### Unit Tests
- `Tag_ShouldRequireName` — verify that a tag with empty name is rejected
- `AddTagToWorkOrderCommand_ShouldAddTag` — verify command adds a tag to the work order
- `RemoveTagFromWorkOrderCommand_ShouldRemoveTag` — verify command removes a tag from the work order
- `WorkOrderManage_ShouldRenderTagsSection` — bUnit test verifying tags section renders with existing tags

### Integration Tests
- `Tag_ShouldPersistAndRetrieve` — create a tag and verify it round-trips through the database
- `WorkOrderTag_ShouldPersistRelationship` — add a tag to a work order and verify the relationship persists
- `TagsQuery_ShouldReturnAllTags` — verify query returns all tags in alphabetical order
- `WorkOrderSearchQuery_FilterByTag_ShouldReturnMatchingResults` — verify filtering by tag returns correct work orders

### Acceptance Tests
- Navigate to a work order, add a new tag "urgent-repair", save, and verify the tag appears on the work order
- Navigate to work order search, filter by tag "urgent-repair", and verify only tagged work orders appear
- Navigate to a work order with tags, remove a tag, and verify it is removed from the display
