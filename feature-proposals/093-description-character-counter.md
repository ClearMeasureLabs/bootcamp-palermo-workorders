## Why
Users entering long descriptions have no visibility into how much space remains before hitting the 4000-character database limit. A live character counter prevents data truncation errors, reduces failed submissions, and provides a smoother form-filling experience.

## What Changes
- Add `CharacterCounter.razor` component in `src/UI/Client/Shared/` that displays current and maximum character count
- Integrate `CharacterCounter` into the work order description textarea on the create and edit forms in `src/UI/Client/Pages/`
- Display format: "123 / 4000" below the description field, updating on each keystroke
- Show warning styling (yellow text) when count exceeds 3600 characters (90% threshold)
- Show error styling (red text) when count reaches 4000 characters
- Prevent additional character input on the client side when 4000 characters reached using `maxlength` attribute and JavaScript interop if needed
- Add server-side validation in `SaveDraftCommand` to reject descriptions exceeding 4000 characters

## Capabilities
### New Capabilities
- Live character count display below description field showing "current / 4000"
- Warning indicator at 90% capacity (3600+ characters)
- Error indicator at 100% capacity (4000 characters)
- Client-side prevention of exceeding 4000-character limit
- Server-side validation rejecting descriptions over 4000 characters

### Modified Capabilities
- Work order create and edit forms updated to include character counter component

## Impact
- **src/UI/Client/Shared/** - New `CharacterCounter.razor` component
- **src/UI/Client/Pages/** - Work order create and edit forms updated to include counter
- **src/Core/Model/StateCommands/** - `SaveDraftCommand` updated with description length validation
- **Dependencies** - No new NuGet packages required
- **Database** - No schema changes required

## Acceptance Criteria
### Unit Tests
- `CharacterCounter_EmptyInput_DisplaysZeroCount` - bUnit render with empty text shows "0 / 4000"
- `CharacterCounter_WithText_DisplaysCorrectCount` - bUnit render with 150-char text shows "150 / 4000"
- `CharacterCounter_At90Percent_ShowsWarningStyle` - bUnit render with 3601 chars applies warning CSS class
- `CharacterCounter_AtLimit_ShowsErrorStyle` - bUnit render with 4000 chars applies error CSS class
- `SaveDraft_DescriptionOver4000_ThrowsValidationException` - Description exceeding 4000 characters throws ValidationException
- `SaveDraft_DescriptionExactly4000_Succeeds` - Description at exactly 4000 characters saves successfully

### Integration Tests
- `SaveDraft_LongDescription_Persisted` - Save work order with 3999-character description, verify it persists correctly
- `SaveDraft_OverLimitDescription_Rejected` - Attempt to save with 4001-character description, verify rejection

### Acceptance Tests
- `DescriptionField_Typing_CounterUpdatesLive` - Navigate to create form, type in description field, verify counter increments with each character via Playwright
- `DescriptionField_NearLimit_ShowsWarning` - Enter 3601 characters in description, verify warning styling visible
- `DescriptionField_AtLimit_PreventsAdditionalInput` - Enter 4000 characters, attempt to type more, verify character count stays at 4000
