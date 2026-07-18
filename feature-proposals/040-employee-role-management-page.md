## Why
Administrators currently have no UI for managing roles and role assignments. A dedicated role management page enables administrators to create, edit, and assign roles without direct database access, improving security and reducing administrative overhead.

## What Changes
- Create a `RolesQuery` in `src/Core/Queries/` to retrieve all roles with their permission flags
- Create a `CreateRoleCommand` in `src/Core/Model/StateCommands/` for adding new roles
- Create an `UpdateRoleCommand` in `src/Core/Model/StateCommands/` for editing role properties (Name, CanCreateWorkOrder, CanFulfillWorkOrder)
- Create an `AssignRoleToEmployeeCommand` and `RemoveRoleFromEmployeeCommand` in `src/Core/Model/StateCommands/`
- Add MediatR handlers in `src/DataAccess/Handlers/` for all new commands and queries
- Create a `RoleManagement` page component in `src/UI/Client/` with sections for listing roles, creating/editing roles, and assigning roles to employees
- Add an `EmployeesByRoleQuery` in `src/Core/Queries/` to show which employees have a given role
- Add API endpoints in `src/UI/Api/` for role CRUD and role-employee assignment
- Restrict access to the role management page to administrators

## Capabilities
### New Capabilities
- Role management admin page listing all roles with their permission flags
- Create new role with Name, CanCreateWorkOrder, and CanFulfillWorkOrder flags
- Edit existing role properties
- Assign a role to an employee from the role management page
- Remove a role from an employee
- View which employees are assigned to each role

### Modified Capabilities
- None

## Impact
- `src/Core/Queries/` — new RolesQuery and EmployeesByRoleQuery
- `src/Core/Model/StateCommands/` — new CreateRoleCommand, UpdateRoleCommand, AssignRoleToEmployeeCommand, RemoveRoleFromEmployeeCommand
- `src/DataAccess/Handlers/` — new MediatR handlers for all commands and queries
- `src/UI/Client/` — new RoleManagement page component
- `src/UI/Api/` — new API endpoints for role management
- No database migration required — uses existing Role and Employee-Role tables
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- RolesQuery handler returns all roles with correct CanCreateWorkOrder and CanFulfillWorkOrder flags
- CreateRoleCommand validates that Name is not empty and not a duplicate
- UpdateRoleCommand updates the specified role's properties
- AssignRoleToEmployeeCommand adds the role to the employee's role collection
- RemoveRoleFromEmployeeCommand removes the role from the employee's role collection
- bUnit test verifies the role management page renders a list of roles with edit controls

### Integration Tests
- Creating a new role persists it to the database
- Updating a role's properties changes the stored values
- Assigning a role to an employee creates the correct association in the database
- Removing a role from an employee deletes the association
- EmployeesByRoleQuery returns the correct employees for a given role

### Acceptance Tests
- Administrator navigates to the Role Management page and sees all existing roles
- Administrator creates a new role with specified permissions and it appears in the list
- Administrator edits a role's CanCreateWorkOrder flag and the change is saved
- Administrator assigns a role to an employee and the employee appears in the role's member list
- Administrator removes a role from an employee and the employee no longer appears in the member list
- Non-administrator users cannot access the Role Management page
