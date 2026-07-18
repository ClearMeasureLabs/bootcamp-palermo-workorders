## Why
Supervisors and administrators need to find work orders created or completed within specific time periods for reporting, auditing, and workload analysis. Date range filtering enables time-based queries that are essential for operational oversight.

## What Changes
- Add `CreatedDateFrom`, `CreatedDateTo`, `CompletedDateFrom`, and `CompletedDateTo` properties to `WorkOrderSpecificationQuery` in `src/Core/Queries/`
- Update the query handler in `src/DataAccess/Handlers/` to filter by date ranges when provided
- Add date picker inputs (from/to pairs) for CreatedDate and CompletedDate on the `WorkOrderSearch` Blazor page in `src/UI/Client/`
- Update the API controller in `src/UI/Api/` to accept date range query parameters

## Capabilities
### New Capabilities
- Filter work orders by CreatedDate range (from/to)
- Filter work orders by CompletedDate range (from/to)
- Date picker UI controls on the WorkOrderSearch page
- Combine date filters with existing search filters

### Modified Capabilities
- WorkOrderSearch page layout updated to include date range filter controls
- WorkOrderSpecificationQuery extended with four new optional date properties

## Impact
- `src/Core/Queries/WorkOrderSpecificationQuery.cs` — four new nullable DateTime properties
- `src/DataAccess/Handlers/` — updated query handler with date range WHERE clauses
- `src/UI/Client/` — updated WorkOrderSearch page with date picker components
- `src/UI/Api/` — updated API controller with date range parameters
- No database migration required — filters use existing CreatedDate and CompletedDate columns
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- Handler filters work orders with CreatedDate on or after CreatedDateFrom
- Handler filters work orders with CreatedDate on or before CreatedDateTo
- Handler filters work orders with CompletedDate within the specified range
- Null date range values result in no date filtering
- Combined CreatedDate and CompletedDate ranges filter correctly

### Integration Tests
- Query returns only work orders within the specified CreatedDate range from a seeded database
- Query returns only work orders within the specified CompletedDate range from a seeded database
- Query with no matching dates returns empty results

### Acceptance Tests
- User sets a CreatedDate from/to range and sees only work orders created within that range
- User sets a CompletedDate from/to range and sees only completed work orders within that range
- Clearing date filters restores the unfiltered results
