## Why
Blank screens when no data exists are confusing and unhelpful. Empty state messages with clear explanations and call-to-action buttons guide users on what to do next, improving discoverability and reducing support requests from new users.

## What Changes
- Add `EmptyState.razor` component in `src/UI.Shared/Components/` with parameters: Title, Description, IconName, ActionButtonText, ActionButtonUrl
- Add SVG illustration assets in `src/UI/Client/wwwroot/images/empty-states/` for: no search results, no work orders, and no notifications
- Modify `WorkOrderSearch.razor` in `src/UI/Client/Pages/` to show `EmptyState` with "No work orders found" message and "Create Work Order" button when search returns zero results
- Add empty state for initial load when no work orders exist in the system: "No work orders yet" with "Create your first work order" call-to-action
- Add empty state for filtered search with no matches: "No results match your filters" with "Clear filters" button
- Add CSS styles for empty state layout (centered content, illustration sizing, button styling)
- Ensure empty state messages are distinct for different scenarios (no data vs. no matches)

## Capabilities
### New Capabilities
- Empty state display on WorkOrderSearch when no work orders exist in the system
- Empty state display on WorkOrderSearch when search/filter returns no results
- Call-to-action buttons in empty states directing users to relevant actions
- SVG illustrations for visual context in empty states
- Reusable `EmptyState` component configurable for any page

### Modified Capabilities
- WorkOrderSearch page shows contextual empty states instead of a blank table

## Impact
- **UI.Shared**: New `EmptyState.razor` component
- **UI.Client**: Modified `WorkOrderSearch.razor`, new SVG assets in `wwwroot/images/empty-states/`
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
- Navigate to WorkOrderSearch with a search term that returns no results and verify the empty state appears with `data-testid="empty-state-no-results"` showing "No results match your filters"
- Verify the "Clear filters" button with `data-testid="empty-state-action"` clears the search and reloads results
- On a system with no work orders, navigate to WorkOrderSearch and verify the empty state appears with `data-testid="empty-state-no-data"` showing "No work orders yet"
- Verify the "Create Work Order" call-to-action button navigates to the WorkOrderManage page for a new work order
