## Why
Users currently have no immediate visual feedback after performing actions like saving a work order or changing its status. Toast notifications provide clear, non-blocking confirmation that actions succeeded or failed, improving the user experience and reducing uncertainty.

## What Changes
- Add `ToastService` in `src/UI/Client/` as a singleton service that exposes methods: `ShowSuccess(message)`, `ShowError(message)`, `ShowWarning(message)`, and `ShowInfo(message)`
- Add `ToastContainer.razor` component in `src/UI.Shared/Components/` that renders a fixed-position container in the top-right corner holding active toast notifications
- Add `Toast.razor` component in `src/UI.Shared/Components/` rendering a single toast with icon, message, close button, and auto-dismiss countdown bar
- Modify `MainLayout.razor` in `src/UI/Client/` to include the `ToastContainer` component
- Modify `WorkOrderManage.razor` in `src/UI/Client/Pages/` to show success toast on save, assign, begin, and complete actions
- Modify `WorkOrderSearch.razor` in `src/UI/Client/Pages/` to show error toast on failed search or data load
- Add CSS styles for toast appearance, slide-in animation, severity colors (green=success, red=error, yellow=warning, blue=info), and dismiss animation
- Auto-dismiss toasts after 5 seconds with a visual countdown progress bar
- Support stacking multiple toasts vertically

## Capabilities
### New Capabilities
- Toast notification popups for success, error, warning, and info messages
- Auto-dismiss after 5 seconds with visible countdown progress bar
- Manual dismiss via close button on each toast
- Stacked toast display supporting multiple simultaneous notifications
- Slide-in/slide-out animations
- Color-coded severity levels with appropriate icons

### Modified Capabilities
- WorkOrderManage page shows toast on successful save, assign, begin work, and complete actions
- WorkOrderSearch page shows toast on data load errors

## Impact
- **UI.Shared**: New `ToastContainer.razor` and `Toast.razor` components
- **UI.Client**: New `ToastService`, modified `MainLayout.razor`, `WorkOrderManage.razor`, `WorkOrderSearch.razor`
- **Database**: No schema changes required
- **Dependencies**: No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `ToastService_ShowSuccess_AddsToastToCollection` - service adds a success toast to its internal collection
- `ToastService_AfterTimeout_RemovesToast` - service removes toast after configured timeout period
- `Toast_RendersMessage_AndIcon` - bUnit test confirming toast displays the message and severity icon
- `Toast_CloseButton_RemovesToast` - bUnit test confirming clicking close removes the toast
- `ToastContainer_RendersMultipleToasts` - bUnit test confirming container stacks multiple active toasts

### Integration Tests
- None (client-side UI component with no server/database interaction)

### Acceptance Tests
- Navigate to WorkOrderManage, save a Draft work order, and verify a success toast appears with `data-testid="toast-success"`
- Verify the toast auto-dismisses after approximately 5 seconds
- Click the close button on a toast with `data-testid="toast-close"` and verify it is removed immediately
- Trigger an error condition (e.g., save with missing required fields) and verify an error toast appears with `data-testid="toast-error"`
- Trigger multiple actions rapidly and verify multiple toasts stack vertically in the `data-testid="toast-container"`
