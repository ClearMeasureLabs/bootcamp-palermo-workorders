## Why
There is currently no dedicated page to view employee details and their work order activity. An employee profile page provides supervisors and administrators with a consolidated view of each employee's information, roles, and performance metrics.

## What Changes
- Create an `EmployeeProfileQuery` in `src/Core/Queries/` that returns employee details along with work order statistics (created count, assigned count, completed count)
- Create a MediatR handler in `src/DataAccess/Handlers/` that joins Employee data with aggregated WorkOrder counts
- Create an `EmployeeProfile` page component in `src/UI/Client/` displaying name, email, roles, and work order statistics
- Add an API endpoint in `src/UI/Api/` for retrieving the employee profile data
- Add navigation links to the employee profile from relevant pages (e.g., work order assignee names)

## Capabilities
### New Capabilities
- Dedicated employee profile page showing FirstName, LastName, EmailAddress, UserName, and Roles
- Work order statistics section showing count of work orders created, assigned, and completed by the employee
- Clickable employee names throughout the application that link to the profile page

### Modified Capabilities
- None

## Impact
- `src/Core/Queries/` — new EmployeeProfileQuery and result model
- `src/DataAccess/Handlers/` — new MediatR handler with aggregate queries
- `src/UI/Client/` — new EmployeeProfile page component
- `src/UI/Api/` — new API endpoint for employee profile
- `src/UI/Client/` — updated employee name links to point to profile page
- No database migration required — uses existing tables with aggregate queries
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- EmployeeProfileQuery handler returns correct employee details for a valid employee
- Handler returns correct created, assigned, and completed work order counts
- Handler returns null or not-found for a non-existent employee
- bUnit test verifies the profile component renders all employee fields and statistics

### Integration Tests
- Query returns correct employee details and aggregated counts from a seeded database
- Counts are accurate when the employee has work orders in multiple statuses

### Acceptance Tests
- User navigates to an employee profile page and sees the employee's name, email, and roles
- Work order statistics (created, assigned, completed counts) are displayed correctly
- User clicks an employee name on a work order page and is navigated to that employee's profile
