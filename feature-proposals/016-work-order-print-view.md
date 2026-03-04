## Why
Maintenance staff sometimes need printed work orders to take to job sites without internet access. A print-optimized view provides a clean, single-page layout with all relevant details, reducing wasted paper and ensuring readability.

## What Changes
- Add `WorkOrderPrint.razor` page to `src/UI.Shared/` (or appropriate UI project) with a print-friendly layout
- Include all work order details: Number, Title, Description, RoomNumber, Status, Creator, Assignee, CreatedDate, AssignedDate, CompletedDate
- Add `@media print` CSS styles that hide navigation, toolbars, and other non-essential UI elements
- Add a "Print" button on the `WorkOrderManage` page that opens the print view or triggers `window.print()`
- Use a clean, minimal layout optimized for standard paper sizes (Letter/A4)
- Include a signature line area at the bottom for on-site verification

## Capabilities
### New Capabilities
- Users can print a work order from the manage page using a dedicated "Print" button
- The print view displays all work order details in a clean, paper-friendly layout
- Non-essential UI elements (navigation, sidebar, buttons) are hidden during printing
- A signature line is included for on-site verification

### Modified Capabilities
- WorkOrderManage page includes a new "Print" button in the action bar

## Impact
- **UI.Shared** — New `WorkOrderPrint.razor` page with print-specific CSS styles; "Print" button added to `WorkOrderManage`
- No backend changes required

## Acceptance Criteria
### Unit Tests
- `WorkOrderPrint_ShouldRenderAllWorkOrderFields` — bUnit test verifying all work order details (Number, Title, Description, Room, Status, Creator, Assignee, dates) are rendered
- `WorkOrderPrint_ShouldRenderSignatureLine` — bUnit test verifying the signature line area is present
- `WorkOrderManage_ShouldRenderPrintButton` — bUnit test verifying "Print" button appears on the manage page

### Integration Tests
- None required — this feature is purely UI-side with no backend data changes

### Acceptance Tests
- Navigate to an existing work order, click the "Print" button, and verify the print view opens with all work order details visible
- Verify the print view does not display navigation elements, sidebar, or action buttons
- Verify the print layout fits on a single page for a standard work order
