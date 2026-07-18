## Why
Assigning work orders to employees who are out of office or on leave causes delays and confusion. Displaying employee availability status during assignment helps supervisors make better decisions and reduces the need to reassign work orders.

## What Changes
- Create an `AvailabilityStatus` smart enum in `src/Core/Model/` with values: Available, Busy, OutOfOffice, OnLeave
- Add an `AvailabilityStatus` property to the `Employee` entity in `src/Core/Model/`
- Create a `ChangeEmployeeAvailabilityCommand` in `src/Core/Model/StateCommands/`
- Create a database migration script in `src/Database/scripts/Update/` to add the AvailabilityStatusId column to the Employee table
- Update the EF Core entity configuration in `src/DataAccess/` for the new property
- Add a MediatR handler in `src/DataAccess/Handlers/` for the availability command
- Update the work order assignment dropdown in `src/UI/Client/` to display availability status next to each employee name
- Show a warning dialog when assigning to an employee with a non-Available status
- Add an API endpoint in `src/UI/Api/` for updating employee availability

## Capabilities
### New Capabilities
- AvailabilityStatus smart enum with Available, Busy, OutOfOffice, and OnLeave values
- Availability status displayed next to employee names in assignment dropdowns
- Warning dialog when attempting to assign a work order to a non-available employee
- Ability for employees or administrators to update availability status

### Modified Capabilities
- Work order assignment dropdown enhanced with availability indicators
- Employee profile page shows current availability status

## Impact
- `src/Core/Model/AvailabilityStatus.cs` — new smart enum
- `src/Core/Model/Employee.cs` — new AvailabilityStatus property
- `src/Core/Model/StateCommands/` — new ChangeEmployeeAvailabilityCommand
- `src/DataAccess/` — updated EF Core configuration, new MediatR handler
- `src/Database/scripts/Update/` — new migration script adding AvailabilityStatusId column
- `src/UI/Client/` — updated assignment dropdown, warning dialog, profile page
- `src/UI/Api/` — new endpoint for availability updates
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- AvailabilityStatus smart enum resolves all four values correctly from code and key
- ChangeEmployeeAvailabilityCommand updates the employee's availability status
- Employee entity defaults to Available status
- bUnit test verifies assignment dropdown shows availability indicator next to each employee

### Integration Tests
- Changing employee availability persists correctly to the database
- Employee query returns the correct availability status
- Migration script adds the column with a default value of Available

### Acceptance Tests
- User sees availability status indicators next to employee names in the assignment dropdown
- User assigns a work order to an unavailable employee and sees a warning dialog
- User can confirm or cancel the assignment after seeing the warning
- User updates their own availability status from their profile page
