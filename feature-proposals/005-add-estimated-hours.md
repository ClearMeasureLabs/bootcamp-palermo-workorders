## Why
Tracking estimated effort for work requests helps with resource planning and workload balancing across team members. Knowing how much time a task is expected to take enables supervisors to distribute work more equitably and plan daily schedules effectively.

## What Changes
- Add `EstimatedHours` nullable `decimal` property to the `WorkRequest` domain model in `src/Core/Model/`
- Update `DataContext` EF Core mapping to persist `EstimatedHours` as a `decimal(5,2)` column
- Add a new DbUp migration script to `src/Database/scripts/Update/` adding a nullable `EstimatedHours` column to the `WorkRequest` table
- Update `WorkRequestManage` Blazor form to include a numeric input for EstimatedHours with validation (must be positive if provided)
- Update `WorkRequestSearch` results to display estimated hours column
- Update query objects to include EstimatedHours in returned data

## Capabilities
### New Capabilities
- Users can enter an optional estimated hours value when creating or editing a work request
- Search results display estimated hours for each work request

### Modified Capabilities
- WorkRequestManage form includes a new EstimatedHours numeric input field
- WorkRequestSearch results table includes an EstimatedHours column

## Impact
- **Core** — `WorkRequest` model gains nullable `EstimatedHours` decimal property
- **DataAccess** — EF Core mapping update for `EstimatedHours` column
- **UI.Shared** — `WorkRequestManage` form updated with numeric input; `WorkRequestSearch` results updated with column
- **Database** — New migration script adding nullable `EstimatedHours` column to `WorkRequest` table

## Acceptance Criteria
### Unit Tests
- `WorkRequest_EstimatedHours_ShouldDefaultToNull` — verify new work requests have no estimated hours by default
- `WorkRequest_EstimatedHours_ShouldRejectNegativeValues` — verify validation rejects negative hour values
- `WorkRequestManage_ShouldRenderEstimatedHoursInput` — bUnit test verifying numeric input appears on the form

### Integration Tests
- `WorkRequest_WithEstimatedHours_ShouldPersistAndRetrieve` — save a work request with 4.5 estimated hours and verify it round-trips through the database
- `WorkRequest_WithNullEstimatedHours_ShouldPersistAndRetrieve` — save a work request without estimated hours and verify null is persisted

### Acceptance Tests
- Navigate to create work request form, enter 3.5 estimated hours, save, and verify the value is displayed on the work request detail page
- Navigate to work request search and verify the estimated hours column displays values for work requests that have them
