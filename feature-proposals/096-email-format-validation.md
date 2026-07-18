## Why
Malformed email addresses stored in Employee records cause notification delivery failures and integration errors with external systems. Validating email format at the point of entry prevents bad data from entering the system and ensures reliable communication channels for work order notifications.

## What Changes
- Add `EmailValidator` static class in `src/Core/Validation/` with `IsValidEmail(string email)` method using standard email format rules (RFC 5322 simplified)
- Add email format validation in `Employee` entity setter or factory method for `EmailAddress` property
- Throw `ValidationException` when email format is invalid during Employee creation or update
- Update Employee creation/update forms in `src/UI/Client/Pages/` with inline validation error "Please enter a valid email address"
- Add client-side email format check using HTML5 `type="email"` attribute and Blazor validation
- Update API endpoints handling Employee data to return 400 Bad Request with validation details for invalid emails

## Capabilities
### New Capabilities
- Server-side email format validation on Employee creation and update
- Client-side inline email validation error on login/profile pages
- Reusable `EmailValidator` utility class for consistent email validation across the codebase
- API returns 400 Bad Request with descriptive error for invalid email format

### Modified Capabilities
- Employee entity updated with email format validation on `EmailAddress` property
- Employee create/update forms updated with inline email validation feedback

## Impact
- **src/Core/Validation/** - New `EmailValidator` class
- **src/Core/Model/** - `Employee` entity updated with email validation in property setter
- **src/Core/Exceptions/** - `ValidationException` used for invalid email errors
- **src/UI/Client/Pages/** - Employee forms updated with inline validation
- **src/UI/Api/** - Endpoint responses updated for email validation errors
- **Dependencies** - No new NuGet packages required
- **Database** - No schema changes required

## Acceptance Criteria
### Unit Tests
- `EmailValidator_ValidEmail_ReturnsTrue` - "user@example.com" returns true
- `EmailValidator_MissingAtSign_ReturnsFalse` - "userexample.com" returns false
- `EmailValidator_MissingDomain_ReturnsFalse` - "user@" returns false
- `EmailValidator_MissingLocalPart_ReturnsFalse` - "@example.com" returns false
- `EmailValidator_EmptyString_ReturnsFalse` - Empty string returns false
- `EmailValidator_MultipleDots_ReturnsTrue` - "user@sub.example.com" returns true
- `Employee_InvalidEmail_ThrowsValidationException` - Setting invalid email on Employee throws ValidationException
- `EmployeeForm_InvalidEmail_ShowsInlineError` - bUnit render with invalid email shows validation error message

### Integration Tests
- `Employee_ValidEmail_PersistsSuccessfully` - Create Employee with valid email, verify persistence
- `Employee_InvalidEmail_NotPersisted` - Attempt to create Employee with invalid email, verify no record created

### Acceptance Tests
- `EmployeeProfile_InvalidEmail_ShowsValidationError` - Navigate to profile page, enter malformed email, submit, verify inline error displayed
- `EmployeeProfile_ValidEmail_SavesSuccessfully` - Enter valid email, submit, verify profile updated without errors
- `Api_CreateEmployee_InvalidEmail_Returns400` - Send POST via Playwright API context with invalid email, verify 400 response with validation error
