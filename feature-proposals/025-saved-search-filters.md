## Why
Users who frequently search with the same filter combinations waste time re-entering criteria each session. Saved search filters allow users to store and quickly recall their common search configurations, improving daily workflow efficiency.

## What Changes
- Create a `SavedSearchFilter` domain entity in `src/Core/Model/` with properties: Id, Name, EmployeeId, FilterJson (serialized filter criteria), CreatedDate
- Add a `SaveSearchFilterCommand` and `DeleteSavedSearchFilterCommand` in `src/Core/Model/StateCommands/`
- Add a `SavedSearchFiltersByEmployeeQuery` in `src/Core/Queries/`
- Create a database migration script to add the `SavedSearchFilter` table
- Add EF Core entity configuration in `src/DataAccess/`
- Add MediatR handlers in `src/DataAccess/Handlers/` for saving, deleting, and retrieving saved filters
- Add a "Save Filter" button and a saved filter dropdown to the `WorkOrderSearch` page in `src/UI/Client/`
- Add API endpoints in `src/UI/Api/` for CRUD operations on saved filters

## Capabilities
### New Capabilities
- Save current search filter combination as a named preset
- Load a previously saved search filter from a dropdown
- Delete saved search filters that are no longer needed
- Each user maintains their own set of saved filters

### Modified Capabilities
- WorkOrderSearch page includes a "Save Filter" button and a saved filter dropdown selector

## Impact
- `src/Core/Model/SavedSearchFilter.cs` — new domain entity
- `src/Core/Model/StateCommands/` — new save and delete commands
- `src/Core/Queries/` — new query for retrieving saved filters by employee
- `src/DataAccess/` — new EF Core entity configuration and DbSet
- `src/DataAccess/Handlers/` — new MediatR handlers
- `src/Database/scripts/Update/` — new migration script to create SavedSearchFilter table
- `src/UI/Client/` — updated WorkOrderSearch page with save/load UI
- `src/UI/Api/` — new API endpoints for saved filter CRUD

## Acceptance Criteria
### Unit Tests
- SavedSearchFilter entity stores Name, EmployeeId, FilterJson, and CreatedDate
- SaveSearchFilterCommand validates that Name is not empty
- DeleteSavedSearchFilterCommand removes the specified filter
- SavedSearchFiltersByEmployeeQuery returns only filters for the specified employee

### Integration Tests
- Saving a filter persists it to the database
- Retrieving filters by employee returns only that employee's filters
- Deleting a filter removes it from the database
- Filter JSON correctly serializes and deserializes search criteria

### Acceptance Tests
- User configures search filters, clicks "Save Filter", enters a name, and the filter is saved
- User selects a saved filter from the dropdown and the search criteria are populated
- User deletes a saved filter and it no longer appears in the dropdown
