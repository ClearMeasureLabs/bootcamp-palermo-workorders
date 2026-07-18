## Why
Supervisors and administrators need to find work requests created or completed within specific time periods for reporting, auditing, and workload analysis. Date range filtering enables time-based queries that are essential for operational oversight.

## What Changes
- Add `CreatedDateFrom`, `CreatedDateTo`, `CompletedDateFrom`, and `CompletedDateTo` properties to `WorkRequestSpecificationQuery` in `src/Core/Queries/`
- Update the query handler in `src/DataAccess/Handlers/` to filter by date ranges when provided
- Add date picker inputs (from/to pairs) for CreatedDate and CompletedDate on the `WorkRequestSearch` Blazor page in `src/UI/Client/`
- Update the API controller in `src/UI/Api/` to accept date range query parameters

## Capabilities
### New Capabilities
- Filter work requests by CreatedDate range (from/to)
- Filter work requests by CompletedDate range (from/to)
- Date picker UI controls on the WorkRequestSearch page
- Combine date filters with existing search filters

### Modified Capabilities
- WorkRequestSearch page layout updated to include date range filter controls
- WorkRequestSpecificationQuery extended with four new optional date properties

## Impact
- `src/Core/Queries/WorkRequestSpecificationQuery.cs` — four new nullable DateTime properties
- `src/DataAccess/Handlers/` — updated query handler with date range WHERE clauses
- `src/UI/Client/` — updated WorkRequestSearch page with date picker components
- `src/UI/Api/` — updated API controller with date range parameters
- No database migration required — filters use existing CreatedDate and CompletedDate columns
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- Handler filters work requests with CreatedDate on or after CreatedDateFrom
- Handler filters work requests with CreatedDate on or before CreatedDateTo
- Handler filters work requests with CompletedDate within the specified range
- Null date range values result in no date filtering
- Combined CreatedDate and CompletedDate ranges filter correctly

### Integration Tests
- Query returns only work requests within the specified CreatedDate range from a seeded database
- Query returns only work requests within the specified CompletedDate range from a seeded database
- Query with no matching dates returns empty results

### Acceptance Tests
- User sets a CreatedDate from/to range and sees only work requests created within that range
- User sets a CompletedDate from/to range and sees only completed work requests within that range
- Clearing date filters restores the unfiltered results
