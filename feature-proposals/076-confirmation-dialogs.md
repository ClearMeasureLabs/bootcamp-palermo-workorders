## Why
Destructive actions like cancelling a work order or applying bulk status updates cannot be undone. A confirmation dialog prevents accidental clicks from causing unintended state changes, protecting data integrity and reducing user errors.

## What Changes
- Add `ConfirmDialog.razor` component in `src/UI.Shared/Components/` rendering a modal dialog with title, message, confirm button, and cancel button
- Add `ConfirmDialogService` in `src/UI/Client/` that provides an async `Confirm(title, message, confirmButtonText)` method returning a `Task<bool>`
- Modify `WorkOrderManage.razor` in `src/UI/Client/Pages/` to show confirmation dialog before executing the Cancel work order action
- Add confirmation dialog support for any future bulk status update operations
- Add CSS styles for the modal overlay (semi-transparent backdrop), dialog box (centered, white background), and button styles (red for destructive confirm, gray for cancel)
- Add keyboard support: Enter to confirm, Escape to cancel
- Add focus trap within the dialog to prevent tabbing to background elements
- Add ARIA attributes for accessibility (`role="dialog"`, `aria-modal="true"`, `aria-labelledby`)

## Capabilities
### New Capabilities
- Reusable confirmation dialog component for any destructive action
- Async service API for programmatic dialog invocation from any page
- Modal overlay preventing interaction with background content
- Keyboard navigation support (Enter to confirm, Escape to cancel)
- Focus trap within dialog for accessibility
- Customizable title, message, and confirm button text

### Modified Capabilities
- Cancel work order action on WorkOrderManage page requires explicit confirmation

## Impact
- **UI.Shared**: New `ConfirmDialog.razor` component
- **UI.Client**: New `ConfirmDialogService`, modified `WorkOrderManage.razor`
- **Database**: No schema changes required
- **Dependencies**: No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `ConfirmDialog_WhenShown_RendersTitle_AndMessage` - bUnit test confirming dialog displays the provided title and message
- `ConfirmDialog_ConfirmButton_ReturnsTrue` - bUnit test confirming clicking confirm resolves the task with true
- `ConfirmDialog_CancelButton_ReturnsFalse` - bUnit test confirming clicking cancel resolves the task with false
- `ConfirmDialog_EscapeKey_ReturnsFalse` - bUnit test confirming Escape key closes dialog and returns false
- `ConfirmDialogService_Confirm_ShowsDialogAndAwaitsResult` - service correctly shows dialog and returns user's choice

### Integration Tests
- None (client-side UI component with no server/database interaction)

### Acceptance Tests
- Navigate to WorkOrderManage for an InProgress work order, click Cancel, and verify the confirmation dialog appears with `data-testid="confirm-dialog"`
- Click the cancel button in the confirmation dialog with `data-testid="confirm-dialog-cancel"` and verify the work order status remains unchanged
- Click Cancel again, then click the confirm button with `data-testid="confirm-dialog-confirm"` and verify the work order status changes to Cancelled
- Verify the dialog has a semi-transparent backdrop with `data-testid="confirm-dialog-overlay"` that prevents clicking background elements
