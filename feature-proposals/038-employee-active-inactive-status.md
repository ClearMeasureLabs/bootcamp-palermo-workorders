## Why
When employees leave the organization or go on extended leave, they should not appear in assignment dropdowns to prevent accidental assignment. An active/inactive status flag enables clean deactivation without deleting employee records or losing historical work order associations.

## What Changes
- Add an `IsActive` boolean property to the `Employee` entity in `src/Core/Model/`, defaulting to true
- Create a database migration script in `src/Database/scripts/Update/` to add the IsActive column to the Employee table with a default value of 1 (true)
- Update the EF Core entity configuration in `src/DataAccess/` for the new property
- Create a `DeactivateEmployeeCommand` and `ReactivateEmployeeCommand` in `src/Core/Model/StateCommands/`
- Add MediatR handlers in `src/DataAccess/Handlers/` for the activation commands
- Update all employee queries used for assignment dropdowns to filter out inactive employees
- Add a visual indicator (badge or styling) for inactive employees on profile and list pages
- Add API endpoints in `src/UI/Api/` for activating and deactivating employees

## Capabilities
### New Capabilities
- IsActive boolean flag on the Employee entity
- DeactivateEmployeeCommand to mark an employee as inactive
- ReactivateEmployeeCommand to restore an inactive employee
- Inactive employees are excluded from work order assignment dropdowns
- Visual indicator on employee profiles and lists showing active/inactive status

### Modified Capabilities
- Employee assignment dropdowns filter out inactive employees
- Employee list pages display active/inactive status

## Impact
- `src/Core/Model/Employee.cs` — new IsActive property
- `src/Core/Model/StateCommands/` — new DeactivateEmployeeCommand and ReactivateEmployeeCommand
- `src/DataAccess/` — updated EF Core configuration, new MediatR handlers
- `src/DataAccess/Handlers/` — updated employee queries to filter by IsActive
- `src/Database/scripts/Update/` — new migration script adding IsActive column with default
- `src/UI/Client/` — updated assignment dropdowns, profile page, employee list styling
- `src/UI/Api/` — new endpoints for activation/deactivation
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- Employee entity defaults IsActive to true
- DeactivateEmployeeCommand sets IsActive to false
- ReactivateEmployeeCommand sets IsActive to true
- Employee queries for assignment dropdowns exclude inactive employees
- bUnit test verifies inactive badge renders on profile component for inactive employees

### Integration Tests
- Deactivating an employee persists IsActive as false in the database
- Reactivating an employee persists IsActive as true
- Assignment dropdown query returns only active employees from a seeded database
- Migration script adds IsActive column with default value of true for existing records

### Acceptance Tests
- Inactive employees do not appear in work order assignment dropdowns
- User views an inactive employee's profile and sees an "Inactive" visual indicator
- Administrator deactivates an employee and they disappear from assignment options
- Administrator reactivates an employee and they reappear in assignment options
