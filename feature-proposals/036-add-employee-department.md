## Why
Organizing employees by department enables departmental filtering during work order assignment, supports departmental reporting, and reflects the organizational structure of the facility management team. This grouping improves assignment accuracy and administrative oversight.

## What Changes
- Create a `Department` entity in `src/Core/Model/` with properties: Id, Name, Description
- Add a `DepartmentId` foreign key property and `Department` navigation property to the `Employee` entity in `src/Core/Model/`
- Create database migration scripts in `src/Database/scripts/Update/` to add the Department table and DepartmentId column to Employee
- Add EF Core entity configurations in `src/DataAccess/` for the Department entity and the Employee-Department relationship
- Add a `DepartmentsQuery` in `src/Core/Queries/` to list all departments
- Add MediatR handlers in `src/DataAccess/Handlers/` for department queries
- Update the employee profile page in `src/UI/Client/` to display the department
- Add a department filter to the work order assignment dropdown in `src/UI/Client/`
- Add API endpoints in `src/UI/Api/` for department retrieval

## Capabilities
### New Capabilities
- Department entity with name and description
- Department displayed on employee profile page
- Department filter on work order assignment dropdown to narrow employee list by department
- Department list query for populating dropdowns

### Modified Capabilities
- Employee entity includes a DepartmentId and Department navigation property
- Assignment dropdown can be filtered by department

## Impact
- `src/Core/Model/Department.cs` — new entity
- `src/Core/Model/Employee.cs` — new DepartmentId and Department properties
- `src/Core/Queries/` — new DepartmentsQuery
- `src/DataAccess/` — new EF Core configuration for Department, updated Employee configuration
- `src/DataAccess/Handlers/` — new MediatR handler for DepartmentsQuery
- `src/Database/scripts/Update/` — new migration scripts for Department table and Employee.DepartmentId column
- `src/UI/Client/` — updated employee profile, updated assignment dropdown with department filter
- `src/UI/Api/` — new API endpoint for departments
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- Department entity stores Name and Description correctly
- Employee entity accepts a DepartmentId
- DepartmentsQuery handler returns all departments sorted by name
- bUnit test verifies department is displayed on the employee profile component

### Integration Tests
- Department records persist correctly to the database
- Employee with a DepartmentId retrieves the correct Department
- Migration scripts create the Department table and add the foreign key without data loss

### Acceptance Tests
- User views an employee profile and sees the department name
- User filters the assignment dropdown by department and sees only employees in that department
- All departments appear in the department filter dropdown
