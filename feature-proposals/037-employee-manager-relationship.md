## Why
Establishing a manager-employee hierarchy enables supervisors to view their direct reports' work orders, supports approval workflows, and provides organizational context for work order management. This relationship is foundational for future features like escalation chains and management dashboards.

## What Changes
- Add a nullable `ManagerId` self-referencing foreign key and `Manager` navigation property to the `Employee` entity in `src/Core/Model/`
- Add a `DirectReports` collection navigation property to the `Employee` entity
- Create a database migration script in `src/Database/scripts/Update/` to add the ManagerId column to the Employee table
- Update the EF Core entity configuration in `src/DataAccess/` to map the self-referencing relationship
- Create a `DirectReportsQuery` in `src/Core/Queries/` to retrieve direct reports for a given manager
- Create a `ManagerChainQuery` in `src/Core/Queries/` to retrieve the reporting chain upward
- Add MediatR handlers in `src/DataAccess/Handlers/` for both queries
- Display the reporting chain on the employee profile page in `src/UI/Client/`
- Allow managers to view all direct reports' work orders from the profile page
- Add API endpoints in `src/UI/Api/` for the new queries

## Capabilities
### New Capabilities
- Manager-employee self-referencing relationship on the Employee entity
- Reporting chain display on employee profile page (employee's manager, manager's manager, etc.)
- Direct reports list on employee profile page for managers
- Managers can navigate to view direct reports' work orders

### Modified Capabilities
- Employee profile page enhanced with reporting chain and direct reports sections

## Impact
- `src/Core/Model/Employee.cs` — new ManagerId, Manager, and DirectReports properties
- `src/Core/Queries/` — new DirectReportsQuery and ManagerChainQuery
- `src/DataAccess/` — updated EF Core Employee configuration for self-reference
- `src/DataAccess/Handlers/` — new MediatR handlers
- `src/Database/scripts/Update/` — new migration script adding ManagerId column
- `src/UI/Client/` — updated employee profile page
- `src/UI/Api/` — new API endpoints
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- Employee entity accepts a nullable ManagerId
- DirectReportsQuery handler returns only direct reports for the specified manager
- ManagerChainQuery handler returns the chain from employee up to the top-level manager
- Employee with no manager returns an empty chain
- bUnit test verifies reporting chain and direct reports render on the profile component

### Integration Tests
- Self-referencing relationship persists and loads correctly from the database
- DirectReportsQuery returns correct employees from a seeded database
- ManagerChainQuery traverses multiple levels correctly
- Migration script adds the nullable ManagerId column without data loss

### Acceptance Tests
- User views an employee profile and sees the reporting chain (manager, manager's manager)
- User views a manager's profile and sees a list of direct reports
- User clicks a direct report name and navigates to that employee's profile
- User can view work orders for a direct report from the manager's profile page
