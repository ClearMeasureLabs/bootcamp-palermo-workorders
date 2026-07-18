## Why
Tracking estimated effort for work orders helps with resource planning and workload balancing across team members. Knowing how much time a task is expected to take enables supervisors to distribute work more equitably and plan daily schedules effectively.

## What Changes
- Add `EstimatedHours` nullable `decimal` property to the `WorkOrder` domain model in `src/Core/Model/`
- Update `DataContext` EF Core mapping to persist `EstimatedHours` as a `decimal(5,2)` column
- Add a new DbUp migration script to `src/Database/scripts/Update/` adding a nullable `EstimatedHours` column to the `WorkOrder` table
- Update `WorkOrderManage` Blazor form to include a numeric input for EstimatedHours with validation (must be positive if provided)
- Update `WorkOrderSearch` results to display estimated hours column
- Update query objects to include EstimatedHours in returned data

## Capabilities
### New Capabilities
- Users can enter an optional estimated hours value when creating or editing a work order
- Search results display estimated hours for each work order

### Modified Capabilities
- WorkOrderManage form includes a new EstimatedHours numeric input field
- WorkOrderSearch results table includes an EstimatedHours column

## Impact
- **Core** — `WorkOrder` model gains nullable `EstimatedHours` decimal property
- **DataAccess** — EF Core mapping update for `EstimatedHours` column
- **UI.Shared** — `WorkOrderManage` form updated with numeric input; `WorkOrderSearch` results updated with column
- **Database** — New migration script adding nullable `EstimatedHours` column to `WorkOrder` table

## Acceptance Criteria
### Unit Tests
- `WorkOrder_EstimatedHours_ShouldDefaultToNull` — verify new work orders have no estimated hours by default
- `WorkOrder_EstimatedHours_ShouldRejectNegativeValues` — verify validation rejects negative hour values
- `WorkOrderManage_ShouldRenderEstimatedHoursInput` — bUnit test verifying numeric input appears on the form

### Integration Tests
- `WorkOrder_WithEstimatedHours_ShouldPersistAndRetrieve` — save a work order with 4.5 estimated hours and verify it round-trips through the database
- `WorkOrder_WithNullEstimatedHours_ShouldPersistAndRetrieve` — save a work order without estimated hours and verify null is persisted

### Acceptance Tests
- Navigate to create work order form, enter 3.5 estimated hours, save, and verify the value is displayed on the work order detail page
- Navigate to work order search and verify the estimated hours column displays values for work orders that have them
