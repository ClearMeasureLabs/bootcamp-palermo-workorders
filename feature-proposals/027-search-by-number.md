## Why
Users who know the specific work request number need a fast way to navigate directly to it without scrolling through search results. A dedicated number search provides instant access and reduces friction for common lookup operations.

## What Changes
- Add a dedicated work request number search input to the `WorkRequestSearch` page in `src/UI/Client/`, separate from the general text search
- Add logic in the Blazor component to detect an exact match on work request number
- When an exact match is found, automatically navigate to the `WorkRequestManage` page for that work request
- When no exact match is found, display a "No work request found with that number" message
- Update the `WorkRequestByNumberQuery` handler in `src/DataAccess/Handlers/` if needed to return a not-found result cleanly

## Capabilities
### New Capabilities
- Dedicated work request number search input field on the WorkRequestSearch page
- Automatic navigation to WorkRequestManage page when an exact number match is found
- Clear feedback message when no work request matches the entered number

### Modified Capabilities
- WorkRequestSearch page layout updated to include a prominent number search field

## Impact
- `src/UI/Client/` — updated WorkRequestSearch page with number search input and navigation logic
- `src/DataAccess/Handlers/` — potential minor update to WorkRequestByNumberQuery handler for clean not-found handling
- No database migration required
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- WorkRequestByNumberQuery handler returns the work request when the number matches
- WorkRequestByNumberQuery handler returns null when no work request matches the number

### Integration Tests
- Query returns the correct work request for an existing number from a seeded database
- Query returns null for a non-existent work request number

### Acceptance Tests
- User enters an existing work request number and is navigated to the WorkRequestManage page for that work request
- User enters a non-existent work request number and sees a "not found" message
- The number search input is visually distinct from the general search input
