## Why
As the number of work orders grows, loading all results at once degrades performance and makes the search page difficult to use. Pagination limits the number of displayed results per page, improving load times and usability.

## What Changes
- Add `Skip`, `Take`, and `PageSize` properties to `WorkOrderSpecificationQuery` in `src/Core/Queries/`
- Create a `PagedResult<T>` generic class in `src/Core/` containing Items, TotalCount, PageNumber, and PageSize
- Update the query handler in `src/DataAccess/Handlers/` to return `PagedResult<WorkOrderSummary>` with server-side pagination using EF Core Skip/Take
- Add page navigation controls (Previous, Next, page numbers) to the `WorkOrderSearch` page in `src/UI/Client/`
- Add a page size selector dropdown (10, 25, 50) to the WorkOrderSearch page
- Update the API controller in `src/UI/Api/` to accept and return pagination parameters

## Capabilities
### New Capabilities
- Server-side pagination of work order search results
- Configurable page size (10, 25, 50 results per page)
- Page navigation controls with Previous, Next, and page number buttons
- Display of total result count and current page indicator

### Modified Capabilities
- WorkOrderSearch page displays a limited number of results per page instead of all results
- WorkOrderSpecificationQuery supports pagination parameters

## Impact
- `src/Core/Queries/WorkOrderSpecificationQuery.cs` — new Skip, Take, PageSize properties
- `src/Core/` — new `PagedResult<T>` class
- `src/DataAccess/Handlers/` — updated query handler with Skip/Take and total count query
- `src/UI/Client/` — updated WorkOrderSearch page with pagination controls and page size selector
- `src/UI/Api/` — updated API controller with pagination parameters and response
- No database migration required
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- Handler returns the correct number of items based on Take value
- Handler skips the correct number of items based on Skip value
- Handler returns accurate TotalCount regardless of pagination
- PagedResult contains correct PageNumber and PageSize values
- Default page size is 10 when not specified

### Integration Tests
- Query returns the first page of results with correct count from a seeded database
- Query returns subsequent pages with correct items
- TotalCount reflects all matching records, not just the current page

### Acceptance Tests
- User sees only 10 work orders by default on the search page
- User changes page size to 25 and sees up to 25 results
- User clicks Next and sees the next page of results
- User clicks a page number and navigates to that page
- Total result count is displayed accurately
