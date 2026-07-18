## Why
Permanently removing cancelled work orders destroys audit trails and historical data needed for reporting and compliance. Soft deletion via an IsArchived flag preserves complete work order history while keeping active views clean, and allows recovery of accidentally archived items.

## What Changes
- Add `IsArchived` boolean property to `WorkOrder` entity in `src/Core/Model/` (default false)
- Add `ArchiveWorkOrderCommand` in `src/Core/Model/StateCommands/` implementing `IStateCommand` that sets `IsArchived = true`
- Add database migration script in `src/Database/scripts/Update/` to add `IsArchived` column to `WorkOrder` table with default value of 0
- Update `AllWorkOrdersQuery` handler in `src/DataAccess/Handlers/` to exclude archived work orders by default
- Add `IncludeArchived` boolean parameter to `AllWorkOrdersQuery` in `src/Core/Queries/`
- Add "Show Archived" toggle button on the work order search/list page in `src/UI/Client/Pages/`
- Replace "Cancel" button with "Archive" button on individual work order pages
- Add global EF Core query filter on `WorkOrder` to exclude `IsArchived == true` by default
- Ensure no permanent delete operation exists; remove any existing hard delete functionality

## Capabilities
### New Capabilities
- Soft delete (archive) work orders instead of permanent deletion
- "Show Archived" toggle on work order list to include/exclude archived items
- Archived work orders visually distinguished with muted styling
- No permanent deletion pathway available to users

### Modified Capabilities
- Cancel action replaced with Archive action on work order detail pages
- Default work order list query excludes archived items
- All work order queries respect the global IsArchived filter unless explicitly overridden

## Impact
- **src/Core/Model/** - `WorkOrder` entity updated with `IsArchived` property
- **src/Core/Model/StateCommands/** - New `ArchiveWorkOrderCommand`
- **src/Core/Queries/** - `AllWorkOrdersQuery` updated with `IncludeArchived` parameter
- **src/DataAccess/** - Global query filter added, handler updated
- **src/Database/** - New migration script adding `IsArchived` column
- **src/UI/Client/Pages/** - Archive button replaces Cancel, "Show Archived" toggle added
- **Dependencies** - No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `ArchiveCommand_SetsIsArchivedTrue` - Executing ArchiveWorkOrderCommand sets IsArchived to true
- `WorkOrder_DefaultIsArchived_IsFalse` - New WorkOrder has IsArchived = false
- `AllWorkOrdersQuery_DefaultExcludesArchived` - Query without IncludeArchived flag does not return archived work orders
- `AllWorkOrdersQuery_IncludeArchived_ReturnsAll` - Query with IncludeArchived = true returns both active and archived work orders
- `WorkOrderList_ShowArchivedToggle_Renders` - bUnit render verifies "Show Archived" toggle is present
- `WorkOrderList_ArchivedToggleOn_ShowsArchivedItems` - bUnit render with toggle on shows archived work orders with muted styling

### Integration Tests
- `ArchiveWorkOrder_PersistsIsArchivedFlag` - Archive a work order, reload from database, verify IsArchived is true
- `DefaultQuery_ExcludesArchivedRecords` - Archive a work order, run default query, verify it is not returned
- `QueryWithIncludeArchived_ReturnsArchivedRecords` - Archive a work order, run query with IncludeArchived, verify it is returned

### Acceptance Tests
- `WorkOrder_ArchiveButton_RemovesFromDefaultList` - Navigate to work order, click Archive, return to list, verify work order no longer visible
- `WorkOrderList_ShowArchivedToggle_RevealsArchivedItems` - Archive a work order, navigate to list, click "Show Archived" toggle, verify archived work order appears
- `WorkOrder_NoDeleteButton_Exists` - Navigate to work order detail, verify no permanent delete button is present
