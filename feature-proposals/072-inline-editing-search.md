## Why
Navigating to the manage page for small edits like changing a title or reassigning a work order is slow for users managing many work orders. Inline editing in the search results table allows quick updates without leaving the list view, significantly improving efficiency.

## What Changes
- Modify `WorkOrderSearch.razor` in `src/UI/Client/Pages/` to support inline editing mode on table rows
- Add `InlineEditableCell.razor` component in `src/UI.Shared/Components/` that toggles between display text and an input field on click
- Add `InlineAssigneeSelect.razor` component in `src/UI.Shared/Components/` that toggles between display text and a dropdown select for assignee
- Add `UpdateWorkOrderInlineCommand` in `src/Core/Model/StateCommands/` for saving inline field changes (title, assignee)
- Add handler for `UpdateWorkOrderInlineCommand` in `src/DataAccess/Handlers/`
- Add edit/save/cancel icon buttons per row that appear on hover or when editing
- Add CSS styles for inline edit mode (highlighted row, input field styling, action buttons)
- Add keyboard support: Enter to save, Escape to cancel inline edit

## Capabilities
### New Capabilities
- Click on a work order title in the search results table to edit it inline
- Click on an assignee cell to change the assignee via inline dropdown
- Save inline changes with Enter key or save button
- Cancel inline edit with Escape key or cancel button
- Visual indication of which row is in edit mode (highlighted background)
- Only one row editable at a time

### Modified Capabilities
- WorkOrderSearch table rows enhanced with inline edit capability for title and assignee columns

## Impact
- **Core**: New `UpdateWorkOrderInlineCommand` state command
- **DataAccess**: New handler for inline update command
- **UI.Shared**: New `InlineEditableCell.razor` and `InlineAssigneeSelect.razor` components
- **UI.Client**: Modified `WorkOrderSearch.razor` with inline edit support
- **Database**: No schema changes required
- **Dependencies**: No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `UpdateWorkOrderInlineCommand_WithNewTitle_UpdatesTitle` - command handler persists title change
- `UpdateWorkOrderInlineCommand_WithNewAssignee_UpdatesAssignee` - command handler persists assignee change
- `InlineEditableCell_OnClick_ShowsInput` - bUnit test confirming cell switches to input mode on click
- `InlineEditableCell_OnEscape_CancelsEdit` - bUnit test confirming Escape reverts to display mode without saving
- `InlineAssigneeSelect_RendersEmployeeOptions` - bUnit test confirming dropdown lists available employees

### Integration Tests
- `UpdateWorkOrderInlineCommand_PersistsTitleChange_ToDatabase` - title change persists through save and reload
- `UpdateWorkOrderInlineCommand_PersistsAssigneeChange_ToDatabase` - assignee change persists through save and reload

### Acceptance Tests
- Navigate to WorkOrderSearch, click on a work order title cell with `data-testid="inline-edit-title"`, change the text, press Enter, and verify the new title is displayed
- Click on an assignee cell with `data-testid="inline-edit-assignee"`, select a different employee from the dropdown, and verify the change saves
- Start editing a title, press Escape, and verify the original title is restored without saving
- Verify only one row can be in edit mode at a time by clicking a second row while the first is being edited
