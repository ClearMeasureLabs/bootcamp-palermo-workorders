## Why
Short or meaningless work request titles like "fix" or "help" make it difficult for maintenance staff to understand and prioritize requests. Enforcing a minimum 5-character title length improves work request clarity and reduces back-and-forth communication between requestors and assignees.

## What Changes
- Add title length validation in `SaveDraftCommand.Execute()` in `src/Core/Model/StateCommands/` that throws a domain validation exception when title is fewer than 5 characters
- Add `ValidationException` class in `src/Core/Exceptions/` if not already present
- Update `StateCommandHandler` in `src/DataAccess/Handlers/` (the existing handler that executes state commands and persists work requests) to catch and return validation errors
- Add client-side validation in the work request creation form in `src/UI/Client/` displaying inline error message "Title must be at least 5 characters"
- Update API controllers to return 400 Bad Request with validation error details when title is too short
- Add `MinimumTitleLength` constant in `WorkRequest` entity or domain constants class

## Capabilities
### New Capabilities
- Server-side validation rejecting work request titles shorter than 5 characters
- Client-side inline validation error message on the work request creation form
- API returns 400 Bad Request with descriptive error for short titles

### Modified Capabilities
- `SaveDraftCommand` execution updated to include title length check
- Work request creation form updated with client-side validation feedback

## Impact
- **src/Core/Model/StateCommands/** - `SaveDraftCommand` updated with title length validation
- **src/Core/Exceptions/** - New or updated `ValidationException` class
- **src/DataAccess/Handlers/** - `StateCommandHandler` updated to handle validation exceptions (note: no dedicated `SaveDraftCommandHandler` exists; `StateCommandHandler` handles all state command requests)
- **src/UI/Client/** - Work request creation form updated with inline validation
- **src/UI/Api/** - Controller returns 400 for validation failures
- **Dependencies** - No new NuGet packages required
- **Database** - No schema changes required

## Acceptance Criteria
### Unit Tests
- `SaveDraft_TitleUnder5Characters_ThrowsValidationException` - Title "fix" (3 chars) throws ValidationException
- `SaveDraft_TitleExactly5Characters_Succeeds` - Title "Fixed" (5 chars) does not throw
- `SaveDraft_TitleOver5Characters_Succeeds` - Title "Fix the broken door" succeeds
- `SaveDraft_EmptyTitle_ThrowsValidationException` - Empty string title throws ValidationException
- `SaveDraft_NullTitle_ThrowsValidationException` - Null title throws ValidationException
- `WorkRequestForm_ShortTitle_DisplaysValidationError` - bUnit render with 3-char title shows inline error message

### Integration Tests
- `SaveDraft_ShortTitle_NotPersistedToDatabase` - Attempt to save draft with 4-char title, verify no work request is persisted
- `SaveDraft_ValidTitle_PersistedToDatabase` - Save draft with valid title, verify work request exists in database

### Acceptance Tests
- `CreateWorkRequest_ShortTitle_ShowsValidationError` - Log in, navigate to create form, enter 3-character title, submit, verify validation error message appears
- `CreateWorkRequest_ValidTitle_SavesSuccessfully` - Enter valid title of 5+ characters, submit, verify work request is created and listed
- `Api_CreateWorkRequest_ShortTitle_Returns400` - Send POST via Playwright API context with 3-char title, verify 400 response with error detail
