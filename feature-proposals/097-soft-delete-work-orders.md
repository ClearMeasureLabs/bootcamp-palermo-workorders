## Why
Permanently removing cancelled work requests destroys audit trails and historical data needed for reporting and compliance. Soft deletion via an IsArchived flag preserves complete work request history while keeping active views clean, and allows recovery of accidentally archived items.

## What Changes
- Add `IsArchived` boolean property to `WorkRequest` entity in `src/Core/Model/` (default false)
- Add `ArchiveWorkRequestCommand` in `src/Core/Model/StateCommands/` implementing `IStateCommand` that sets `IsArchived = true`
- Add database migration script in `src/Database/scripts/Update/` to add `IsArchived` column to `WorkRequest` table with default value of 0
- Update `AllWorkRequestsQuery` handler in `src/DataAccess/Handlers/` to exclude archived work requests by default
- Add `IncludeArchived` boolean parameter to `AllWorkRequestsQuery` in `src/Core/Queries/`
- Add "Show Archived" toggle button on the work request search/list page in `src/UI/Client/Pages/`
- Replace "Cancel" button with "Archive" button on individual work request pages
- Add global EF Core query filter on `WorkRequest` to exclude `IsArchived == true` by default
- Ensure no permanent delete operation exists; remove any existing hard delete functionality

## Capabilities
### New Capabilities
- Soft delete (archive) work requests instead of permanent deletion
- "Show Archived" toggle on work request list to include/exclude archived items
- Archived work requests visually distinguished with muted styling
- No permanent deletion pathway available to users

### Modified Capabilities
- Cancel action replaced with Archive action on work request detail pages
- Default work request list query excludes archived items
- All work request queries respect the global IsArchived filter unless explicitly overridden

## Impact
- **src/Core/Model/** - `WorkRequest` entity updated with `IsArchived` property
- **src/Core/Model/StateCommands/** - New `ArchiveWorkRequestCommand`
- **src/Core/Queries/** - `AllWorkRequestsQuery` updated with `IncludeArchived` parameter
- **src/DataAccess/** - Global query filter added, handler updated
- **src/Database/** - New migration script adding `IsArchived` column
- **src/UI/Client/Pages/** - Archive button replaces Cancel, "Show Archived" toggle added
- **Dependencies** - No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `ArchiveCommand_SetsIsArchivedTrue` - Executing ArchiveWorkRequestCommand sets IsArchived to true
- `WorkRequest_DefaultIsArchived_IsFalse` - New WorkRequest has IsArchived = false
- `AllWorkRequestsQuery_DefaultExcludesArchived` - Query without IncludeArchived flag does not return archived work requests
- `AllWorkRequestsQuery_IncludeArchived_ReturnsAll` - Query with IncludeArchived = true returns both active and archived work requests
- `WorkRequestList_ShowArchivedToggle_Renders` - bUnit render verifies "Show Archived" toggle is present
- `WorkRequestList_ArchivedToggleOn_ShowsArchivedItems` - bUnit render with toggle on shows archived work requests with muted styling

### Integration Tests
- `ArchiveWorkRequest_PersistsIsArchivedFlag` - Archive a work request, reload from database, verify IsArchived is true
- `DefaultQuery_ExcludesArchivedRecords` - Archive a work request, run default query, verify it is not returned
- `QueryWithIncludeArchived_ReturnsArchivedRecords` - Archive a work request, run query with IncludeArchived, verify it is returned

### Acceptance Tests
- `WorkRequest_ArchiveButton_RemovesFromDefaultList` - Navigate to work request, click Archive, return to list, verify work request no longer visible
- `WorkRequestList_ShowArchivedToggle_RevealsArchivedItems` - Archive a work request, navigate to list, click "Show Archived" toggle, verify archived work request appears
- `WorkRequest_NoDeleteButton_Exists` - Navigate to work request detail, verify no permanent delete button is present
