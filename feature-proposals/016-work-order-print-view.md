## Why
Maintenance staff sometimes need printed work requests to take to job sites without internet access. A print-optimized view provides a clean, single-page layout with all relevant details, reducing wasted paper and ensuring readability.

## What Changes
- Add `WorkRequestPrint.razor` page to `src/UI.Shared/` (or appropriate UI project) with a print-friendly layout
- Include all work request details: Number, Title, Description, RoomNumber, Status, Creator, Assignee, CreatedDate, AssignedDate, CompletedDate
- Add `@media print` CSS styles that hide navigation, toolbars, and other non-essential UI elements
- Add a "Print" button on the `WorkRequestManage` page that opens the print view or triggers `window.print()`
- Use a clean, minimal layout optimized for standard paper sizes (Letter/A4)
- Include a signature line area at the bottom for on-site verification

## Capabilities
### New Capabilities
- Users can print a work request from the manage page using a dedicated "Print" button
- The print view displays all work request details in a clean, paper-friendly layout
- Non-essential UI elements (navigation, sidebar, buttons) are hidden during printing
- A signature line is included for on-site verification

### Modified Capabilities
- WorkRequestManage page includes a new "Print" button in the action bar

## Impact
- **UI.Shared** — New `WorkRequestPrint.razor` page with print-specific CSS styles; "Print" button added to `WorkRequestManage`
- No backend changes required

## Acceptance Criteria
### Unit Tests
- `WorkRequestPrint_ShouldRenderAllWorkRequestFields` — bUnit test verifying all work request details (Number, Title, Description, Room, Status, Creator, Assignee, dates) are rendered
- `WorkRequestPrint_ShouldRenderSignatureLine` — bUnit test verifying the signature line area is present
- `WorkRequestManage_ShouldRenderPrintButton` — bUnit test verifying "Print" button appears on the manage page

### Integration Tests
- None required — this feature is purely UI-side with no backend data changes

### Acceptance Tests
- Navigate to an existing work request, click the "Print" button, and verify the print view opens with all work request details visible
- Verify the print view does not display navigation elements, sidebar, or action buttons
- Verify the print layout fits on a single page for a standard work request
