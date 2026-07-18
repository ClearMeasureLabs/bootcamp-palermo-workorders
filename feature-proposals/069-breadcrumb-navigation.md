## Why
Breadcrumb navigation helps users understand their current location within the application hierarchy and provides quick one-click access to parent pages. This is especially important as the application grows with more pages and deeper navigation paths.

## What Changes
- Add `Breadcrumb.razor` component in `src/UI.Shared/Components/` rendering an ordered list of navigation links representing the current page hierarchy
- Add `BreadcrumbItem` record in `src/UI.Shared/` with properties: Label, Url, IsActive
- Add `BreadcrumbService` in `src/UI/Client/` that builds the breadcrumb trail based on the current route and page metadata
- Modify `MainLayout.razor` in `src/UI/Client/` to include the `Breadcrumb` component below the header and above the page content
- Add `[Breadcrumb]` attribute or parameter to each page component specifying its display name and parent route
- Update `WorkOrderSearch.razor` breadcrumb: Home > Work Orders > Search
- Update `WorkOrderManage.razor` breadcrumb: Home > Work Orders > {WorkOrderNumber} (or "New Work Order")
- Add CSS styles for breadcrumb separator characters and active/inactive link states
- Add ARIA `nav` landmark with `aria-label="breadcrumb"` for accessibility

## Capabilities
### New Capabilities
- Breadcrumb trail displayed on all pages below the header
- Clickable breadcrumb links for quick navigation to parent pages
- Dynamic breadcrumb for work order detail pages showing the work order number
- Accessible breadcrumb with proper ARIA attributes
- Automatic breadcrumb generation based on page route hierarchy

### Modified Capabilities
- MainLayout updated to include breadcrumb component
- All page components annotated with breadcrumb metadata

## Impact
- **UI.Shared**: New `Breadcrumb.razor` component, `BreadcrumbItem` record
- **UI.Client**: New `BreadcrumbService`, modified `MainLayout.razor`, updated page components with breadcrumb parameters
- **Database**: No schema changes required
- **Dependencies**: No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `Breadcrumb_RendersAllItems_InOrder` - bUnit test confirming breadcrumb items render in correct hierarchical order
- `Breadcrumb_LastItem_IsNotClickable` - bUnit test confirming the current page breadcrumb item is not a link
- `BreadcrumbService_ForSearchPage_ReturnsCorrectTrail` - service returns Home > Work Orders > Search
- `BreadcrumbService_ForManagePage_IncludesWorkOrderNumber` - service returns trail with dynamic work order number

### Integration Tests
- None (client-side rendering feature with no server/database interaction)

### Acceptance Tests
- Navigate to WorkOrderSearch page and verify breadcrumb displays "Home > Work Orders > Search" with `data-testid="breadcrumb"`
- Click the "Home" breadcrumb link and verify navigation to the home page
- Navigate to a specific work order and verify breadcrumb shows "Home > Work Orders > {number}" with the correct work order number
- Verify the last breadcrumb item (current page) is displayed as text, not a clickable link
