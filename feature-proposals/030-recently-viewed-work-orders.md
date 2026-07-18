## Why
Users frequently revisit the same work requests they recently accessed. Displaying a list of recently viewed work requests provides quick navigation and reduces repetitive searching, improving daily workflow efficiency for maintenance staff.

## What Changes
- Add a JavaScript interop service in `src/UI/Client/` to read and write a list of recently viewed work request numbers to browser localStorage
- Update the `WorkRequestManage` page in `src/UI/Client/` to record the viewed work request number in localStorage on page load
- Create a `RecentlyViewedWorkRequests` Blazor component in `src/UI/Client/` that reads from localStorage and displays the last 10 viewed work requests with links
- Add the `RecentlyViewedWorkRequests` component to the navigation sidebar or dashboard area
- Limit the stored list to 10 entries, removing the oldest when a new entry is added

## Capabilities
### New Capabilities
- Automatic tracking of the last 10 work requests viewed by the current user
- "Recently Viewed" section in navigation showing work request numbers and titles as links
- Click a recently viewed item to navigate directly to that work request
- Data persisted in browser localStorage (no server storage required)

### Modified Capabilities
- WorkRequestManage page records the current work request to the recently viewed list on load
- Navigation layout updated to include the RecentlyViewedWorkRequests component

## Impact
- `src/UI/Client/` — new RecentlyViewedWorkRequests component, new JS interop service, updated WorkRequestManage page
- `src/UI/Client/wwwroot/` — potential JavaScript file for localStorage interop
- No database migration required
- No server-side changes required
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- RecentlyViewedWorkRequests component renders the correct number of items from the provided list
- Component renders work request numbers as clickable links
- Component displays a "No recent work requests" message when the list is empty
- bUnit test verifies the component renders up to 10 items

### Integration Tests
- None required — feature is entirely client-side using browser localStorage

### Acceptance Tests
- User navigates to a work request and it appears in the "Recently Viewed" section
- User views multiple work requests and they appear in reverse chronological order
- The list shows a maximum of 10 items; the oldest is removed when an 11th is viewed
- User clicks a recently viewed item and navigates to that work request
