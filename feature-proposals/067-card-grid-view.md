## Why
A card/grid layout provides a more visual, scannable overview of work orders compared to a dense table. Different users prefer different layouts depending on their workflow, and offering both views improves usability across roles.

## What Changes
- Add `WorkOrderCard.razor` component in `src/UI.Shared/Components/` displaying a summary card with status badge, title, assignee avatar/initials, room number, and created date
- Add `ViewToggle.razor` component in `src/UI.Shared/Components/` with table/grid toggle buttons
- Modify `WorkOrderSearch.razor` in `src/UI/Client/Pages/` to support both table and card grid layouts, controlled by the `ViewToggle` component
- Add CSS grid styles in `src/UI/Client/wwwroot/css/` for the card layout with responsive column counts (1 column on mobile, 2 on tablet, 3-4 on desktop)
- Add `StatusBadge.razor` component in `src/UI.Shared/Components/` rendering a color-coded badge for each `WorkOrderStatus` value
- Store view preference (table/grid) in `localStorage` to persist across sessions

## Capabilities
### New Capabilities
- Card/grid view for work order search results showing visual summary cards
- Status badge component with color coding (Draft=gray, Assigned=blue, InProgress=orange, Complete=green, Cancelled=red)
- Toggle between table and card grid views
- View preference persisted in `localStorage`
- Responsive grid layout adapting to screen size

### Modified Capabilities
- WorkOrderSearch page supports two display modes (table remains the default)

## Impact
- **UI.Shared**: New `WorkOrderCard.razor`, `ViewToggle.razor`, and `StatusBadge.razor` components
- **UI.Client**: Modified `WorkOrderSearch.razor`, new CSS grid styles
- **Database**: No schema changes required
- **Dependencies**: No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `WorkOrderCard_RendersTitle_AndStatus` - bUnit test confirming card displays work order title and status badge
- `WorkOrderCard_RendersAssignee_WhenAssigned` - bUnit test confirming assignee name appears on card
- `StatusBadge_RendersCorrectColor_ForEachStatus` - bUnit test confirming each status maps to the correct CSS class
- `ViewToggle_WhenGridClicked_EmitsGridMode` - bUnit test confirming toggle emits the correct view mode event

### Integration Tests
- None (client-side rendering feature with no new server/database interaction)

### Acceptance Tests
- Navigate to WorkOrderSearch page and verify the view toggle is present with `data-testid="view-toggle"`
- Click the grid view button with `data-testid="view-toggle-grid"` and verify work orders display as cards with `data-testid="work-order-card"`
- Click the table view button with `data-testid="view-toggle-table"` and verify work orders display in the original table format
- Switch to grid view, refresh the page, and verify the grid view persists
- Verify each card shows a status badge with `data-testid="status-badge"`
