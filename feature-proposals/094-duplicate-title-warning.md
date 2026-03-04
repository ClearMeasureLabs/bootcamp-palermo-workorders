## Why
Users sometimes submit duplicate work orders for the same issue, leading to wasted effort and confusion about which request to track. A duplicate title warning alerts users before submission, reducing redundant work orders while still allowing intentional duplicates when the user confirms.

## What Changes
- Add `DuplicateTitleCheckQuery` in `src/Core/Queries/` that accepts a title string and returns matching active work orders (status not Cancelled or Complete)
- Add `DuplicateTitleCheckHandler` in `src/DataAccess/Handlers/` performing case-insensitive title search against active work orders
- Add duplicate check invocation in the work order creation form in `src/UI/Client/Pages/` triggered on title field blur
- Display warning banner below the title field listing matching work orders with their numbers and statuses
- Allow the user to dismiss the warning and proceed with creation
- Add `CheckDuplicateTitle` API endpoint in `src/UI/Api/` returning matching work order summaries

## Capabilities
### New Capabilities
- Real-time duplicate title detection triggered when user leaves the title field
- Warning banner displaying matching active work order numbers and statuses
- User can dismiss warning and proceed with intentional duplicate creation
- API endpoint for programmatic duplicate title checking

### Modified Capabilities
- Work order creation form updated with duplicate check on title blur event

## Impact
- **src/Core/Queries/** - New `DuplicateTitleCheckQuery` and result type
- **src/DataAccess/Handlers/** - New `DuplicateTitleCheckHandler`
- **src/UI/Client/Pages/** - Work order creation form updated with duplicate warning banner
- **src/UI/Api/** - New duplicate check endpoint
- **Dependencies** - No new NuGet packages required
- **Database** - No schema changes required

## Acceptance Criteria
### Unit Tests
- `DuplicateTitleCheck_ExactMatch_ReturnsMatchingWorkOrder` - Query with existing title returns the matching work order
- `DuplicateTitleCheck_CaseInsensitive_ReturnsMatch` - Query with different casing still finds match
- `DuplicateTitleCheck_NoMatch_ReturnsEmptyList` - Query with unique title returns empty list
- `DuplicateTitleCheck_CancelledWorkOrder_ExcludedFromResults` - Cancelled work order with same title not returned
- `DuplicateTitleCheck_CompletedWorkOrder_ExcludedFromResults` - Completed work order with same title not returned
- `CreateForm_DuplicateDetected_ShowsWarningBanner` - bUnit render triggers blur with duplicate title, verify warning banner renders with work order number

### Integration Tests
- `DuplicateTitleCheck_PersistedDuplicate_FoundByQuery` - Seed work order with title, execute query with same title, verify match returned
- `DuplicateTitleCheck_MultipleDuplicates_AllReturned` - Seed three work orders with same title, verify all three returned

### Acceptance Tests
- `CreateWorkOrder_DuplicateTitle_ShowsWarning` - Log in, navigate to create form, enter title matching existing work order, tab out of title field, verify warning banner appears with matching work order number
- `CreateWorkOrder_DuplicateWarning_DismissAndProceed` - Trigger duplicate warning, dismiss it, submit form, verify work order is created
- `CreateWorkOrder_UniqueTitle_NoWarning` - Enter unique title, tab out of field, verify no warning banner appears
