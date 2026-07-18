## Why
The current work order form lacks clear inline validation feedback, causing users to submit invalid data and receive generic error responses. Field-level validation messages with visual indicators help users correct errors before submission, reducing frustration and failed save attempts.

## What Changes
- Add `ValidationMessage.razor` component in `src/UI.Shared/Components/` rendering an error message below an input field with red text styling
- Add `ValidatedInput.razor` component in `src/UI.Shared/Components/` wrapping an input field with validation state (red border on error, green border on valid after interaction)
- Add `WorkOrderValidator` service in `src/Core/` implementing validation rules: Title required (max 300 chars per existing EF mapping), Description max 4000 chars (per existing EF mapping), RoomNumber optional (max 50 chars, nullable per current domain model), Assignee required when status is Assigned or beyond
- Modify `WorkOrderManage.razor` in `src/UI/Client/Pages/` to use `ValidatedInput` components and trigger validation on blur and form submit
- Add `ValidationResult` record in `src/Core/` with properties: FieldName, ErrorMessage, IsValid
- Add CSS styles for validation states: red border and red error text for invalid fields, green border for valid fields after user interaction, neutral border for untouched fields
- Prevent form submission when any validation errors exist
- Show validation summary at the top of the form listing all current errors

## Capabilities
### New Capabilities
- Inline field-level validation messages displayed below each invalid field
- Red border on invalid fields, green border on valid fields after interaction
- Validation triggers on blur (when user leaves a field) and on form submit
- Validation summary at the top of the form listing all errors
- Form submission blocked when validation errors exist
- Reusable `ValidatedInput` and `ValidationMessage` components for any form

### Modified Capabilities
- WorkOrderManage form enhanced with inline validation for all fields

## Impact
- **Core**: New `WorkOrderValidator` service, `ValidationResult` record
- **UI.Shared**: New `ValidationMessage.razor` and `ValidatedInput.razor` components
- **UI.Client**: Modified `WorkOrderManage.razor`
- **Database**: No schema changes required
- **Dependencies**: No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `WorkOrderValidator_WithEmptyTitle_ReturnsError` - validator returns error for blank title
- `WorkOrderValidator_WithTitleExceeding300Chars_ReturnsError` - validator returns error for title over 300-character max length (per existing EF mapping in WorkOrderMap.cs)
- `WorkOrderValidator_WithDescriptionExceeding4000Chars_ReturnsError` - validator returns error for description over 4000-character max length
- `WorkOrderValidator_WithBlankRoomNumber_NoError` - validator does NOT return error for blank room number (RoomNumber is nullable per current domain model)
- `WorkOrderValidator_WithAssignedStatusAndNoAssignee_ReturnsError` - validator returns error when status requires assignee but none is set
- `WorkOrderValidator_WithValidFields_ReturnsNoErrors` - validator returns empty error collection for valid input
- `ValidatedInput_OnBlurWithInvalidValue_ShowsErrorBorder` - bUnit test confirming red border appears after blur with invalid value
- `ValidationMessage_WithError_RendersMessage` - bUnit test confirming error message text renders below input

### Integration Tests
- None (client-side validation with no new server/database interaction; server-side validation already exists)

### Acceptance Tests
- Navigate to WorkOrderManage for a new work order, leave the Title field empty, tab to the next field, and verify a validation message appears with `data-testid="validation-error-title"`
- Enter a title longer than 300 characters and verify the validation message indicates the maximum length with `data-testid="validation-error-title"`
- Fill in all required fields correctly and verify no validation messages are displayed
- Click Save with an empty required field and verify the validation summary appears at the top with `data-testid="validation-summary"` listing all errors
- Correct all errors and verify the Save button submits successfully
