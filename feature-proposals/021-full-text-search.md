## Why
Maintenance staff need to quickly find work orders by keywords in titles and descriptions, especially when they cannot remember the exact work order number. Full-text search reduces time spent scrolling through lists and improves operational efficiency.

## What Changes
- Add a `SearchText` property to `WorkOrderSpecificationQuery` in `src/Core/Queries/`
- Update the query handler in `src/DataAccess/Handlers/` to apply SQL `LIKE` filtering against `Title` and `Description` columns
- Add a search text input field to the `WorkOrderSearch` Blazor page in `src/UI/Client/`
- Wire the search input to the query via the API controller in `src/UI/Api/`
- Debounce the search input to avoid excessive queries on each keystroke

## Capabilities
### New Capabilities
- Free-text search across work order Title and Description fields
- Search input on the WorkOrderSearch page that filters results as the user types
- Case-insensitive partial matching using SQL LIKE

### Modified Capabilities
- WorkOrderSearch page now includes a text search input above the results grid
- WorkOrderSpecificationQuery now accepts an optional SearchText parameter

## Impact
- `src/Core/Queries/WorkOrderSpecificationQuery.cs` — new property
- `src/DataAccess/Handlers/` — updated query handler with LIKE filtering
- `src/UI/Client/` — updated WorkOrderSearch page component
- `src/UI/Api/` — updated API controller to accept search text parameter
- No database migration required — uses existing columns with LIKE operator
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- WorkOrderSpecificationQuery handler returns only work orders whose Title contains the search text
- WorkOrderSpecificationQuery handler returns only work orders whose Description contains the search text
- WorkOrderSpecificationQuery handler returns work orders matching in either Title or Description
- Empty or null SearchText returns all work orders (no filter applied)
- Search is case-insensitive

### Integration Tests
- Query handler filters work orders by SearchText against a seeded database
- Results include matches in Title, Description, or both
- No results returned when SearchText matches nothing

### Acceptance Tests
- User navigates to WorkOrderSearch, enters text in the search input, and sees only matching work orders
- Clearing the search input restores the full list of work orders
- Search matches partial strings within Title and Description
