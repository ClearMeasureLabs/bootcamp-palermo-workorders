## Why
Uneven distribution of work orders across assignees leads to burnout and missed deadlines. A workload chart gives managers instant visibility into each assignee's active load, enabling balanced reassignment and more equitable work distribution.

## What Changes
- Add `WorkloadByAssigneeQuery` to `src/Core/Queries/` returning a list of `(Employee Assignee, int ActiveCount)` where active means status is Assigned or InProgress
- Add `WorkloadByAssigneeHandler` in `src/DataAccess/Handlers/` grouping active work orders by `Assignee` and counting
- Add `AssigneeWorkloadChart.razor` component in `src/UI/Client/` rendering a horizontal bar chart
- Add API endpoint in `src/UI/Api/`
- Display the chart on the dashboard page

## Capabilities
### New Capabilities
- Horizontal bar chart showing number of active work orders (Assigned + InProgress) per assignee
- Bars labeled with employee name and count
- Color coding to highlight overloaded assignees exceeding a configurable threshold

### Modified Capabilities
- Dashboard page updated to include the workload chart

## Impact
- `src/Core/` — new query class
- `src/DataAccess/` — new handler with EF Core group-by join query
- `src/UI/Client/` — new chart component with JS interop for rendering
- `src/UI/Api/` — new API endpoint
- No database schema changes required
- May require a client-side charting library (e.g., Chart.js via JS interop) — requires approval per project conventions

## Acceptance Criteria
### Unit Tests
- `WorkloadByAssigneeHandler` returns correct counts per assignee for a known dataset
- `WorkloadByAssigneeHandler` excludes completed and cancelled work orders from counts
- `WorkloadByAssigneeHandler` includes assignees with zero active work orders if they have historical assignments
- `AssigneeWorkloadChart` component renders bar elements for each assignee using bUnit

### Integration Tests
- `WorkloadByAssigneeHandler` returns accurate results from a seeded database with multiple assignees and varied statuses
- Query correctly groups by assignee identity

### Acceptance Tests
- Navigate to the dashboard and verify the workload chart is visible
- Assign a work order to an employee and verify the chart updates on refresh
- Complete a work order and verify the assignee's count decreases on refresh
