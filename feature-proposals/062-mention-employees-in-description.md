## Why
Allowing @username mentions in work request descriptions enables direct communication about responsibilities and context within the work request itself. Mentioned employees can quickly see they are referenced, improving coordination and accountability.

## What Changes
- Add `MentionParser` service in `src/Core/` that extracts @username tokens from description text and resolves them against known employee usernames
- Add `MentionLinkRenderer` component in `src/UI.Shared/Components/` that renders parsed mentions as clickable links to employee profiles
- Modify `WorkRequestManage.razor` in `src/UI/Client/` to highlight @mentions in the description textarea with a styled preview
- Add `GetEmployeeByUsernameQuery` in `src/Core/Queries/` to resolve mentioned usernames
- Add handler for `GetEmployeeByUsernameQuery` in `src/DataAccess/Handlers/`
- Add `EmployeeMentionAutoComplete` component in `src/UI.Shared/Components/` that shows a dropdown of matching usernames when typing `@`
- Add CSS styles for mention highlighting (blue text, underline on hover)

## Capabilities
### New Capabilities
- Parse @username patterns in work request description fields
- Render mentioned usernames as clickable links to employee profile/details
- Autocomplete dropdown when typing `@` in description field showing matching employee usernames
- Visual highlighting of mentioned usernames in description display

### Modified Capabilities
- WorkRequestManage page description field enhanced with mention autocomplete
- Work request detail display renders mentions as styled links

## Impact
- **Core**: New `MentionParser` service interface, `GetEmployeeByUsernameQuery`
- **DataAccess**: New handler for username lookup query
- **UI.Shared**: New `MentionLinkRenderer` and `EmployeeMentionAutoComplete` components
- **UI.Client**: Modified `WorkRequestManage.razor` for mention input support
- **Dependencies**: No new NuGet packages required
- **Database**: No schema changes required (mentions stored as plain text in existing Description field)

## Acceptance Criteria
### Unit Tests
- `MentionParser_WithSingleMention_ExtractsUsername` - parses `@jsmith` from description text
- `MentionParser_WithMultipleMentions_ExtractsAllUsernames` - parses multiple distinct @mentions
- `MentionParser_WithNoMentions_ReturnsEmptyList` - returns empty collection for plain text
- `MentionParser_WithInvalidUsername_ExcludesFromResults` - ignores @mentions that do not match any employee
- `MentionLinkRenderer_WithValidMention_RendersLink` - bUnit test confirming anchor tag rendered for valid mention
- `EmployeeMentionAutoComplete_WithPartialInput_ShowsMatchingEmployees` - bUnit test confirming dropdown filters correctly

### Integration Tests
- `GetEmployeeByUsernameQuery_WithExistingUsername_ReturnsEmployee` - query returns correct employee record
- `GetEmployeeByUsernameQuery_WithNonExistentUsername_ReturnsNull` - query returns null for unknown username

### Acceptance Tests
- Type `@` in the description field on WorkRequestManage page and verify autocomplete dropdown appears with `data-testid="mention-autocomplete"`
- Select a username from the autocomplete and verify it is inserted into the description field
- Save a work request with an @mention in the description, reload, and verify the mention renders as a clickable link with `data-testid="mention-link"`
