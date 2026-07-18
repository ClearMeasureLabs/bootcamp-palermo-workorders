## Why
Matching employees to work orders based on their skills and specialties improves first-time fix rates and reduces reassignment. Tracking skills enables smarter assignment decisions and provides data for workforce planning and training initiatives.

## What Changes
- Create a `Skill` entity in `src/Core/Model/` with properties: Id, Name, Description
- Create an `EmployeeSkill` join entity in `src/Core/Model/` linking Employee to Skill (many-to-many)
- Optionally create a `WorkOrderCategory` entity in `src/Core/Model/` with properties: Id, Name, and a collection of associated Skills
- Create database migration scripts in `src/Database/scripts/Update/` for the Skill, EmployeeSkill, and optionally WorkOrderCategory tables
- Add EF Core entity configurations in `src/DataAccess/` for the new entities
- Add queries in `src/Core/Queries/` to retrieve employees by skill and to list all skills
- Add MediatR handlers in `src/DataAccess/Handlers/` for skill-related queries and commands
- Update the work order assignment UI in `src/UI/Client/` to suggest employees based on matching skills
- Add a skill management section to the employee profile page
- Add API endpoints in `src/UI/Api/` for skill CRUD and employee-skill assignment

## Capabilities
### New Capabilities
- Skill entity with name and description
- Many-to-many relationship between Employee and Skill via EmployeeSkill
- Skill-based employee suggestions when assigning work orders
- Skill management on employee profile (add/remove skills)
- List of all available skills for selection

### Modified Capabilities
- Work order assignment dropdown enhanced with skill-based suggestions
- Employee profile page includes a skills section

## Impact
- `src/Core/Model/Skill.cs` — new entity
- `src/Core/Model/EmployeeSkill.cs` — new join entity
- `src/Core/Model/Employee.cs` — new Skills navigation property
- `src/Core/Queries/` — new queries for skills and employee-skill lookups
- `src/DataAccess/` — new EF Core configurations, updated Employee configuration
- `src/DataAccess/Handlers/` — new MediatR handlers
- `src/Database/scripts/Update/` — new migration scripts for Skill and EmployeeSkill tables
- `src/UI/Client/` — updated assignment UI and employee profile
- `src/UI/Api/` — new API endpoints for skill management

## Acceptance Criteria
### Unit Tests
- Skill entity stores Name and Description correctly
- EmployeeSkill correctly links an Employee and a Skill
- Employee can have multiple skills
- Query handler returns employees matching a specified skill
- bUnit test verifies the skills section renders on the employee profile

### Integration Tests
- Skill and EmployeeSkill records persist correctly to the database
- Query returns employees filtered by skill from a seeded database
- Adding and removing skills from an employee updates the join table correctly

### Acceptance Tests
- User views an employee profile and sees their listed skills
- User adds a skill to an employee and it appears in the skills section
- User removes a skill from an employee and it is no longer displayed
- During work order assignment, employees with matching skills are suggested first
