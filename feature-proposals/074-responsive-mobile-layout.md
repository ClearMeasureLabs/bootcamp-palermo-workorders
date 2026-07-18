## Why
Field workers and supervisors often access the work order system from mobile devices while on-site. The current layout is desktop-oriented, making it difficult to navigate and interact on smaller screens. Responsive design ensures the application is usable across all device sizes.

## What Changes
- Modify `NavMenu.razor` in `src/UI/Client/Shared/` to implement a hamburger menu that collapses on screens narrower than 768px, with a slide-out overlay menu on tap
- Add `HamburgerButton.razor` component in `src/UI.Shared/Components/` rendering the hamburger icon button visible only on mobile viewports
- Modify `WorkOrderSearch.razor` in `src/UI/Client/Pages/` to stack filter fields vertically on mobile, make the results table horizontally scrollable, and hide lower-priority columns (RoomNumber, CreatedDate) on small screens
- Modify `WorkOrderManage.razor` in `src/UI/Client/Pages/` to use full-width form fields on mobile with increased touch target sizes for buttons and inputs
- Add responsive CSS media queries in `src/UI/Client/wwwroot/css/app.css` for breakpoints at 480px (phone), 768px (tablet), and 1024px (desktop)
- Add viewport meta tag if not already present in `index.html`
- Increase button and input min-height to 44px on mobile for touch accessibility
- Add touch-friendly spacing between interactive elements

## Capabilities
### New Capabilities
- Hamburger menu for navigation on mobile and tablet viewports
- Horizontally scrollable search results table on small screens
- Stacked filter layout on mobile for WorkOrderSearch
- Full-width form fields on mobile for WorkOrderManage
- Touch-friendly button and input sizes (minimum 44px tap targets)

### Modified Capabilities
- NavMenu collapses to hamburger menu below 768px viewport width
- WorkOrderSearch adapts layout based on screen size
- WorkOrderManage form fields expand to full width on mobile

## Impact
- **UI.Shared**: New `HamburgerButton.razor` component
- **UI.Client**: Modified `NavMenu.razor`, `WorkOrderSearch.razor`, `WorkOrderManage.razor`, updated `app.css` with responsive media queries
- **Database**: No schema changes required
- **Dependencies**: No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `HamburgerButton_WhenClicked_TogglesMenuVisibility` - bUnit test confirming hamburger button toggles the nav menu open/closed state
- `HamburgerButton_OnDesktop_IsHidden` - bUnit test confirming hamburger button is not rendered at desktop viewport context

### Integration Tests
- None (client-side CSS/layout feature with no server/database interaction)

### Acceptance Tests
- Set browser viewport to 480px width and verify the hamburger menu icon appears with `data-testid="hamburger-menu"`
- Tap the hamburger menu and verify the navigation slides out with `data-testid="mobile-nav-menu"`
- At 480px viewport, navigate to WorkOrderSearch and verify filters are stacked vertically and the table scrolls horizontally with `data-testid="search-results-table"`
- At 480px viewport, navigate to WorkOrderManage and verify form fields span full width
- At 1024px viewport, verify the standard sidebar navigation is visible and the hamburger menu is hidden
