## Why
Users sometimes submit duplicate work requests for the same issue, leading to wasted effort and confusion about which request to track. A duplicate title warning alerts users before submission, reducing redundant work requests while still allowing intentional duplicates when the user confirms.

## What Changes
- Add `DuplicateTitleCheckQuery` in `src/Core/Queries/` that accepts a title string and returns matching active work requests (status not Cancelled or Complete)
- Add `DuplicateTitleCheckHandler` in `src/DataAccess/Handlers/` performing case-insensitive title search against active work requests
- Add duplicate check invocation in the work request creation form in `src/UI/Client/Pages/` triggered on title field blur
- Display warning banner below the title field listing matching work requests with their numbers and statuses
- Allow the user to dismiss the warning and proceed with creation
- Add `CheckDuplicateTitle` API endpoint in `src/UI/Api/` returning matching work request summaries

## Capabilities
### New Capabilities
- Real-time duplicate title detection triggered when user leaves the title field
- Warning banner displaying matching active work request numbers and statuses
- User can dismiss warning and proceed with intentional duplicate creation
- API endpoint for programmatic duplicate title checking

### Modified Capabilities
- Work request creation form updated with duplicate check on title blur event

## Impact
- **src/Core/Queries/** - New `DuplicateTitleCheckQuery` and result type
- **src/DataAccess/Handlers/** - New `DuplicateTitleCheckHandler`
- **src/UI/Client/Pages/** - Work request creation form updated with duplicate warning banner
- **src/UI/Api/** - New duplicate check endpoint
- **Dependencies** - No new NuGet packages required
- **Database** - No schema changes required

## Acceptance Criteria
### Unit Tests
- `DuplicateTitleCheck_ExactMatch_ReturnsMatchingWorkRequest` - Query with existing title returns the matching work request
- `DuplicateTitleCheck_CaseInsensitive_ReturnsMatch` - Query with different casing still finds match
- `DuplicateTitleCheck_NoMatch_ReturnsEmptyList` - Query with unique title returns empty list
- `DuplicateTitleCheck_CancelledWorkRequest_ExcludedFromResults` - Cancelled work request with same title not returned
- `DuplicateTitleCheck_CompletedWorkRequest_ExcludedFromResults` - Completed work request with same title not returned
- `CreateForm_DuplicateDetected_ShowsWarningBanner` - bUnit render triggers blur with duplicate title, verify warning banner renders with work request number

### Integration Tests
- `DuplicateTitleCheck_PersistedDuplicate_FoundByQuery` - Seed work request with title, execute query with same title, verify match returned
- `DuplicateTitleCheck_MultipleDuplicates_AllReturned` - Seed three work requests with same title, verify all three returned

### Acceptance Tests
- `CreateWorkRequest_DuplicateTitle_ShowsWarning` - Log in, navigate to create form, enter title matching existing work request, tab out of title field, verify warning banner appears with matching work request number
- `CreateWorkRequest_DuplicateWarning_DismissAndProceed` - Trigger duplicate warning, dismiss it, submit form, verify work request is created
- `CreateWorkRequest_UniqueTitle_NoWarning` - Enter unique title, tab out of field, verify no warning banner appears
