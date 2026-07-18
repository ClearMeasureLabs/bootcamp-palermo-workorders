## Why
Users frequently revisit the same work orders they recently accessed. Displaying a list of recently viewed work orders provides quick navigation and reduces repetitive searching, improving daily workflow efficiency for maintenance staff.

## What Changes
- Add a JavaScript interop service in `src/UI/Client/` to read and write a list of recently viewed work order numbers to browser localStorage
- Update the `WorkOrderManage` page in `src/UI/Client/` to record the viewed work order number in localStorage on page load
- Create a `RecentlyViewedWorkOrders` Blazor component in `src/UI/Client/` that reads from localStorage and displays the last 10 viewed work orders with links
- Add the `RecentlyViewedWorkOrders` component to the navigation sidebar or dashboard area
- Limit the stored list to 10 entries, removing the oldest when a new entry is added

## Capabilities
### New Capabilities
- Automatic tracking of the last 10 work orders viewed by the current user
- "Recently Viewed" section in navigation showing work order numbers and titles as links
- Click a recently viewed item to navigate directly to that work order
- Data persisted in browser localStorage (no server storage required)

### Modified Capabilities
- WorkOrderManage page records the current work order to the recently viewed list on load
- Navigation layout updated to include the RecentlyViewedWorkOrders component

## Impact
- `src/UI/Client/` — new RecentlyViewedWorkOrders component, new JS interop service, updated WorkOrderManage page
- `src/UI/Client/wwwroot/` — potential JavaScript file for localStorage interop
- No database migration required
- No server-side changes required
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- RecentlyViewedWorkOrders component renders the correct number of items from the provided list
- Component renders work order numbers as clickable links
- Component displays a "No recent work orders" message when the list is empty
- bUnit test verifies the component renders up to 10 items

### Integration Tests
- None required — feature is entirely client-side using browser localStorage

### Acceptance Tests
- User navigates to a work order and it appears in the "Recently Viewed" section
- User views multiple work orders and they appear in reverse chronological order
- The list shows a maximum of 10 items; the oldest is removed when an 11th is viewed
- User clicks a recently viewed item and navigates to that work order
