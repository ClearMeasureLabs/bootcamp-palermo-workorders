## Why
Blank screens when no data exists are confusing and unhelpful. Empty state messages with clear explanations and call-to-action buttons guide users on what to do next, improving discoverability and reducing support requests from new users.

## What Changes
- Add `EmptyState.razor` component in `src/UI.Shared/Components/` with parameters: Title, Description, IconName, ActionButtonText, ActionButtonUrl
- Add SVG illustration assets in `src/UI/Client/wwwroot/images/empty-states/` for: no search results, no work requests, and no notifications
- Modify `WorkRequestSearch.razor` in `src/UI/Client/Pages/` to show `EmptyState` with "No work requests found" message and "Create Work Request" button when search returns zero results
- Add empty state for initial load when no work requests exist in the system: "No work requests yet" with "Create your first work request" call-to-action
- Add empty state for filtered search with no matches: "No results match your filters" with "Clear filters" button
- Add CSS styles for empty state layout (centered content, illustration sizing, button styling)
- Ensure empty state messages are distinct for different scenarios (no data vs. no matches)

## Capabilities
### New Capabilities
- Empty state display on WorkRequestSearch when no work requests exist in the system
- Empty state display on WorkRequestSearch when search/filter returns no results
- Call-to-action buttons in empty states directing users to relevant actions
- SVG illustrations for visual context in empty states
- Reusable `EmptyState` component configurable for any page

### Modified Capabilities
- WorkRequestSearch page shows contextual empty states instead of a blank table

## Impact
- **UI.Shared**: New `EmptyState.razor` component
- **UI.Client**: Modified `WorkRequestSearch.razor`, new SVG assets in `wwwroot/images/empty-states/`
- **Database**: No schema changes required
- **Dependencies**: No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `EmptyState_RendersTitle_AndDescription` - bUnit test confirming component displays the provided title and description
- `EmptyState_WithActionButton_RendersLink` - bUnit test confirming call-to-action button renders with correct text and URL
- `EmptyState_WithoutActionButton_HidesButton` - bUnit test confirming button is not rendered when no ActionButtonText is provided
- `EmptyState_RendersIcon` - bUnit test confirming the SVG illustration renders

### Integration Tests
- None (client-side UI component with no server/database interaction)

### Acceptance Tests
- Navigate to WorkRequestSearch with a search term that returns no results and verify the empty state appears with `data-testid="empty-state-no-results"` showing "No results match your filters"
- Verify the "Clear filters" button with `data-testid="empty-state-action"` clears the search and reloads results
- On a system with no work requests, navigate to WorkRequestSearch and verify the empty state appears with `data-testid="empty-state-no-data"` showing "No work requests yet"
- Verify the "Create Work Request" call-to-action button navigates to the WorkRequestManage page for a new work request
