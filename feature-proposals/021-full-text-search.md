## Why
Maintenance staff need to quickly find work requests by keywords in titles and descriptions, especially when they cannot remember the exact work request number. Full-text search reduces time spent scrolling through lists and improves operational efficiency.

## What Changes
- Add a `SearchText` property to `WorkRequestSpecificationQuery` in `src/Core/Queries/`
- Update the query handler in `src/DataAccess/Handlers/` to apply SQL `LIKE` filtering against `Title` and `Description` columns
- Add a search text input field to the `WorkRequestSearch` Blazor page in `src/UI/Client/`
- Wire the search input to the query via the API controller in `src/UI/Api/`
- Debounce the search input to avoid excessive queries on each keystroke

## Capabilities
### New Capabilities
- Free-text search across work request Title and Description fields
- Search input on the WorkRequestSearch page that filters results as the user types
- Case-insensitive partial matching using SQL LIKE

### Modified Capabilities
- WorkRequestSearch page now includes a text search input above the results grid
- WorkRequestSpecificationQuery now accepts an optional SearchText parameter

## Impact
- `src/Core/Queries/WorkRequestSpecificationQuery.cs` — new property
- `src/DataAccess/Handlers/` — updated query handler with LIKE filtering
- `src/UI/Client/` — updated WorkRequestSearch page component
- `src/UI/Api/` — updated API controller to accept search text parameter
- No database migration required — uses existing columns with LIKE operator
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- WorkRequestSpecificationQuery handler returns only work requests whose Title contains the search text
- WorkRequestSpecificationQuery handler returns only work requests whose Description contains the search text
- WorkRequestSpecificationQuery handler returns work requests matching in either Title or Description
- Empty or null SearchText returns all work requests (no filter applied)
- Search is case-insensitive

### Integration Tests
- Query handler filters work requests by SearchText against a seeded database
- Results include matches in Title, Description, or both
- No results returned when SearchText matches nothing

### Acceptance Tests
- User navigates to WorkRequestSearch, enters text in the search input, and sees only matching work requests
- Clearing the search input restores the full list of work requests
- Search matches partial strings within Title and Description
