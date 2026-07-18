## Why
Flexible tagging allows work requests to be grouped by ad-hoc criteria beyond fixed categories, supporting cross-cutting organization like "budget-approved", "safety-related", or "recurring". Tags provide a user-driven classification system that adapts to evolving organizational needs.

## What Changes
- Add `Tag` entity to `src/Core/Model/` with properties: Id (Guid), Name (string, unique)
- Add `WorkRequestTag` join entity to `src/Core/Model/` with properties: WorkRequestId (Guid), TagId (Guid)
- Add navigation property `Tags` (ICollection<Tag>) to `WorkRequest` domain model via many-to-many relationship
- Add `TagsQuery` to `src/Core/Queries/` to retrieve all available tags
- Add `AddTagToWorkRequestCommand` and `RemoveTagFromWorkRequestCommand` to `src/Core/Model/StateCommands/`
- Add EF Core mappings for `Tag` and `WorkRequestTag` join table in DataAccess
- Add handlers for tag commands and queries in `src/DataAccess/Handlers/`
- Add two new DbUp migration scripts: one for the `Tag` table and one for the `WorkRequestTag` join table
- Add a tag management component on the `WorkRequestManage` page with typeahead/autocomplete for existing tags
- Add a tag filter to `WorkRequestSearch` page

## Capabilities
### New Capabilities
- Users can add and remove tags on any work request
- Users can create new tags inline when adding a tag that does not exist
- Users can filter work requests by tag on the search page
- Tags are displayed as badges on work request search results

### Modified Capabilities
- WorkRequestManage page includes a new tags section with add/remove functionality
- WorkRequestSearch results display tags and support tag-based filtering

## Impact
- **Core** — New `Tag` entity; new `WorkRequestTag` join entity; new commands and query
- **DataAccess** — EF Core mappings for `Tag` and `WorkRequestTag` tables; new MediatR handlers for tag operations
- **UI.Shared** — Tag management component on `WorkRequestManage`; tag filter on `WorkRequestSearch`
- **Database** — Two new migration scripts creating `Tag` and `WorkRequestTag` tables

## Acceptance Criteria
### Unit Tests
- `Tag_ShouldRequireName` — verify that a tag with empty name is rejected
- `AddTagToWorkRequestCommand_ShouldAddTag` — verify command adds a tag to the work request
- `RemoveTagFromWorkRequestCommand_ShouldRemoveTag` — verify command removes a tag from the work request
- `WorkRequestManage_ShouldRenderTagsSection` — bUnit test verifying tags section renders with existing tags

### Integration Tests
- `Tag_ShouldPersistAndRetrieve` — create a tag and verify it round-trips through the database
- `WorkRequestTag_ShouldPersistRelationship` — add a tag to a work request and verify the relationship persists
- `TagsQuery_ShouldReturnAllTags` — verify query returns all tags in alphabetical order
- `WorkRequestSearchQuery_FilterByTag_ShouldReturnMatchingResults` — verify filtering by tag returns correct work requests

### Acceptance Tests
- Navigate to a work request, add a new tag "urgent-repair", save, and verify the tag appears on the work request
- Navigate to work request search, filter by tag "urgent-repair", and verify only tagged work requests appear
- Navigate to a work request with tags, remove a tag, and verify it is removed from the display
