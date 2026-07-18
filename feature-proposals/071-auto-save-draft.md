## Why
Users can lose unsaved work if they navigate away or experience a browser issue while editing a work request. Auto-saving drafts at regular intervals protects against data loss and provides a smoother editing experience without requiring manual saves.

## What Changes
- Add `AutoSaveService` in `src/UI/Client/` that monitors form field changes on `WorkRequestManage.razor` and triggers a save command after 30 seconds of inactivity (debounced)
- Add `SaveDraftCommand` in `src/Core/Model/StateCommands/` that persists the current work request form state without changing its status
- Add handler for `SaveDraftCommand` in `src/DataAccess/Handlers/` that saves only for work requests in Draft status
- Modify `WorkRequestManage.razor` in `src/UI/Client/Pages/` to integrate the `AutoSaveService` and display an auto-save status indicator
- Add `AutoSaveIndicator.razor` component in `src/UI.Shared/Components/` showing "Auto-saved at {time}", "Saving...", or "Unsaved changes" states
- Add JavaScript interop for detecting form field changes via `input` events and resetting the debounce timer
- Ensure auto-save only triggers for work requests in Draft status (not for Assigned, InProgress, or other statuses)

## Capabilities
### New Capabilities
- Automatic saving of draft work requests every 30 seconds after the last change
- Debounced save to avoid excessive API calls during rapid editing
- Visual indicator showing auto-save status (saved, saving, unsaved changes)
- Auto-save timestamp displayed to confirm when the last save occurred
- Auto-save disabled for non-Draft work requests

### Modified Capabilities
- WorkRequestManage page enhanced with auto-save behavior and indicator for Draft work requests

## Impact
- **Core**: New `SaveDraftCommand` state command
- **DataAccess**: New handler for `SaveDraftCommand`
- **UI.Client**: New `AutoSaveService`, modified `WorkRequestManage.razor`, new JS interop
- **UI.Shared**: New `AutoSaveIndicator.razor` component
- **Database**: No schema changes required
- **Dependencies**: No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `SaveDraftCommand_ForDraftWorkRequest_PersistsChanges` - command handler saves field changes for Draft work requests
- `SaveDraftCommand_ForNonDraftWorkRequest_DoesNotSave` - command handler skips save for non-Draft statuses
- `AutoSaveIndicator_WhenSaved_ShowsTimestamp` - bUnit test confirming indicator displays "Auto-saved at {time}"
- `AutoSaveIndicator_WhenSaving_ShowsSavingState` - bUnit test confirming indicator displays "Saving..."
- `AutoSaveIndicator_WhenUnsaved_ShowsUnsavedState` - bUnit test confirming indicator displays "Unsaved changes"

### Integration Tests
- `SaveDraftCommand_PersistsFieldChanges_ToDatabase` - title and description changes persist through save and reload
- `SaveDraftCommand_DoesNotChangeStatus_FromDraft` - work request remains in Draft status after auto-save

### Acceptance Tests
- Navigate to WorkRequestManage for a Draft work request, modify the title, wait 30 seconds, and verify the auto-save indicator with `data-testid="auto-save-indicator"` shows "Auto-saved"
- Modify the description field and verify the indicator changes to "Unsaved changes" with `data-testid="auto-save-indicator"`
- Navigate to a work request in Assigned status and verify the auto-save indicator is not displayed
