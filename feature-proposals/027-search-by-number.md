## Why
Users who know the specific work order number need a fast way to navigate directly to it without scrolling through search results. A dedicated number search provides instant access and reduces friction for common lookup operations.

## What Changes
- Add a dedicated work order number search input to the `WorkOrderSearch` page in `src/UI/Client/`, separate from the general text search
- Add logic in the Blazor component to detect an exact match on work order number
- When an exact match is found, automatically navigate to the `WorkOrderManage` page for that work order
- When no exact match is found, display a "No work order found with that number" message
- Update the `WorkOrderByNumberQuery` handler in `src/DataAccess/Handlers/` if needed to return a not-found result cleanly

## Capabilities
### New Capabilities
- Dedicated work order number search input field on the WorkOrderSearch page
- Automatic navigation to WorkOrderManage page when an exact number match is found
- Clear feedback message when no work order matches the entered number

### Modified Capabilities
- WorkOrderSearch page layout updated to include a prominent number search field

## Impact
- `src/UI/Client/` — updated WorkOrderSearch page with number search input and navigation logic
- `src/DataAccess/Handlers/` — potential minor update to WorkOrderByNumberQuery handler for clean not-found handling
- No database migration required
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- WorkOrderByNumberQuery handler returns the work order when the number matches
- WorkOrderByNumberQuery handler returns null when no work order matches the number

### Integration Tests
- Query returns the correct work order for an existing number from a seeded database
- Query returns null for a non-existent work order number

### Acceptance Tests
- User enters an existing work order number and is navigated to the WorkOrderManage page for that work order
- User enters a non-existent work order number and sees a "not found" message
- The number search input is visually distinct from the general search input
