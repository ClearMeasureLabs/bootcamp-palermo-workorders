## Why
Common work order operations like assigning, beginning work, or completing require navigating to the manage page. A context menu with quick actions on each search result row reduces navigation overhead and allows batch-style workflow management directly from the list view.

## What Changes
- Add `QuickActionsMenu.razor` component in `src/UI.Shared/Components/` rendering a dropdown menu with contextual actions based on the work order's current status
- Add `QuickActionButton.razor` component in `src/UI.Shared/Components/` rendering an ellipsis (three-dot) button that opens the menu on click
- Modify `WorkOrderSearch.razor` in `src/UI/Client/Pages/` to include a `QuickActionButton` in each table row
- Add `CloneWorkOrderCommand` in `src/Core/Model/StateCommands/` that creates a new Draft work order copying title, description, and room number from an existing work order
- Add handler for `CloneWorkOrderCommand` in `src/DataAccess/Handlers/`
- Add logic to show/hide actions based on current status: View (always), Edit (Draft/Assigned), Assign (Draft), Begin (Assigned), Complete (InProgress), Clone (always)
- Add right-click context menu support via JavaScript interop intercepting the `contextmenu` event on table rows
- Add CSS styles for the dropdown menu positioning, hover states, and dividers between action groups

## Capabilities
### New Capabilities
- Quick actions dropdown menu on each work order row in search results
- Context menu triggered by right-click on a row or clicking the action button
- Contextual actions filtered by current work order status
- Clone action creates a duplicate Draft work order from any existing work order
- Actions: View, Edit, Assign, Begin Work, Complete, Clone

### Modified Capabilities
- WorkOrderSearch table rows enhanced with quick action button column

## Impact
- **Core**: New `CloneWorkOrderCommand` state command
- **DataAccess**: New handler for clone command
- **UI.Shared**: New `QuickActionsMenu.razor` and `QuickActionButton.razor` components
- **UI.Client**: Modified `WorkOrderSearch.razor`, new JS interop for context menu
- **Database**: No schema changes required
- **Dependencies**: No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `CloneWorkOrderCommand_CreatesNewDraft_WithCopiedFields` - command handler creates a new work order with same title, description, and room number but Draft status
- `CloneWorkOrderCommand_GeneratesNewNumber` - cloned work order gets a new unique number
- `QuickActionsMenu_ForDraftWorkOrder_ShowsAssignAction` - bUnit test confirming Assign action is visible for Draft status
- `QuickActionsMenu_ForInProgressWorkOrder_ShowsCompleteAction` - bUnit test confirming Complete action is visible for InProgress status
- `QuickActionsMenu_ForCompletedWorkOrder_HidesEditAction` - bUnit test confirming Edit action is hidden for Complete status
- `QuickActionsMenu_AlwaysShowsViewAndClone` - bUnit test confirming View and Clone are always visible

### Integration Tests
- `CloneWorkOrderCommand_PersistsClonedWorkOrder_ToDatabase` - cloned work order exists in database with correct field values
- `CloneWorkOrderCommand_DoesNotModifyOriginal` - original work order remains unchanged after cloning

### Acceptance Tests
- Navigate to WorkOrderSearch, click the action button with `data-testid="quick-action-button"` on a row, and verify the dropdown menu appears with `data-testid="quick-actions-menu"`
- Click "Clone" from the quick actions menu and verify a new Draft work order is created with the same title
- Verify that a Draft work order row shows "Assign" in its quick actions menu
- Verify that an InProgress work order row shows "Complete" in its quick actions menu
- Right-click on a table row and verify the context menu appears with the same quick actions
