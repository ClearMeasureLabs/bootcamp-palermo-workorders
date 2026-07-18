## Why
Supervisors need visibility into each employee's current workload to make informed assignment decisions and balance work distribution. A workload dashboard prevents overloading individuals and helps identify employees with capacity to take on new work orders.

## What Changes
- Create an `EmployeeWorkloadQuery` in `src/Core/Queries/` that returns a list of employees with their current Assigned and InProgress work order counts
- Create a MediatR handler in `src/DataAccess/Handlers/` that aggregates work order counts grouped by assignee and status
- Create an `EmployeeWorkloadDashboard` page component in `src/UI/Client/` displaying a table or bar chart of employee workloads
- Add an API endpoint in `src/UI/Api/` for retrieving workload data
- Add navigation to the dashboard from the main menu

## Capabilities
### New Capabilities
- Employee workload dashboard page showing all employees and their current work order counts
- Table view with columns: Employee Name, Assigned Count, InProgress Count, Total Active Count
- Optional bar chart visualization of workload distribution
- Dashboard accessible from the main navigation menu

### Modified Capabilities
- None

## Impact
- `src/Core/Queries/` — new EmployeeWorkloadQuery and EmployeeWorkloadResult model
- `src/DataAccess/Handlers/` — new MediatR handler with GROUP BY aggregation
- `src/UI/Client/` — new EmployeeWorkloadDashboard page component
- `src/UI/Api/` — new API endpoint for workload data
- `src/UI/Client/` — updated navigation to include dashboard link
- No database migration required — uses existing tables with aggregate queries
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- EmployeeWorkloadQuery handler returns correct Assigned count per employee
- Handler returns correct InProgress count per employee
- Handler returns zero counts for employees with no active work orders
- Employees without any work orders are included in the results
- bUnit test verifies the dashboard component renders employee rows with correct counts

### Integration Tests
- Query returns correct workload data from a seeded database with multiple employees and work orders
- Counts update correctly when work order statuses change

### Acceptance Tests
- User navigates to the Employee Workload Dashboard from the main menu
- Dashboard displays a table with all employees and their Assigned and InProgress counts
- Workload data reflects the current state of work orders in the system
