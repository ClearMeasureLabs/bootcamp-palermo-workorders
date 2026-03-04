## Why
Maintenance coordinators need to reach employees by phone for urgent work order updates. Storing phone numbers in the system eliminates the need for separate contact lists and ensures contact information is readily available alongside work order assignments.

## What Changes
- Add a `PhoneNumber` property (nullable string) to the `Employee` entity in `src/Core/Model/`
- Create a database migration script in `src/Database/scripts/Update/` to add the PhoneNumber column to the Employee table
- Update the EF Core entity configuration in `src/DataAccess/` to map the PhoneNumber property
- Update the employee login/profile display in `src/UI/Client/` to show the phone number
- Update any employee creation or editing flows to include a phone number input field
- Add phone number validation (format check) in the domain model or command

## Capabilities
### New Capabilities
- PhoneNumber field on the Employee entity
- Phone number displayed on employee profile and relevant UI pages
- Phone number input field on employee editing forms
- Basic phone number format validation

### Modified Capabilities
- Employee display components updated to show phone number when available

## Impact
- `src/Core/Model/Employee.cs` — new PhoneNumber property
- `src/DataAccess/` — updated EF Core entity configuration for PhoneNumber mapping
- `src/Database/scripts/Update/` — new migration script adding PhoneNumber column (nullable VARCHAR)
- `src/UI/Client/` — updated employee display components and forms
- `src/UI/Api/` — updated employee-related API models to include PhoneNumber
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- Employee entity accepts a valid phone number string
- Employee entity accepts null phone number (field is optional)
- Phone number validation rejects clearly invalid formats
- bUnit test verifies phone number is rendered on employee display component

### Integration Tests
- Employee with a phone number is persisted to and retrieved from the database correctly
- Employee without a phone number (null) is persisted and retrieved correctly
- Migration script adds the PhoneNumber column without affecting existing data

### Acceptance Tests
- User views an employee profile and sees the phone number displayed
- User edits an employee profile and can update the phone number
- Phone number field accepts valid formats and rejects invalid input
