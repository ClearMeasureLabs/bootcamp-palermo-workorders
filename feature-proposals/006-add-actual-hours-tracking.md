## Why
Recording actual time spent on work orders enables comparison with estimates, identifies efficiency bottlenecks, and supports reporting on labor allocation. This data is essential for improving future estimates and understanding true operational costs.

## What Changes
- Add `ActualHours` nullable `decimal` property to the `WorkOrder` domain model in `src/Core/Model/`
- Update `DataContext` EF Core mapping to persist `ActualHours` as a `decimal(5,2)` column
- Add a new DbUp migration script to `src/Database/scripts/Update/` adding a nullable `ActualHours` column to the `WorkOrder` table
- Update `WorkOrderManage` Blazor form to include a numeric input for ActualHours, editable only when status is InProgress or Complete
- Update `WorkOrderSearch` results to display actual hours column
- Add domain validation: ActualHours can only be set when status is InProgress or Complete

## Capabilities
### New Capabilities
- Users can record actual hours spent on a work order when it is in InProgress or Complete status
- Search results display actual hours for each work order

### Modified Capabilities
- WorkOrderManage form includes a new ActualHours numeric input field with status-based editability
- WorkOrderSearch results table includes an ActualHours column

## Impact
- **Core** — `WorkOrder` model gains nullable `ActualHours` decimal property with status-based validation
- **DataAccess** — EF Core mapping update for `ActualHours` column
- **UI.Shared** — `WorkOrderManage` form updated with conditional numeric input; `WorkOrderSearch` results updated with column
- **Database** — New migration script adding nullable `ActualHours` column to `WorkOrder` table

## Acceptance Criteria
### Unit Tests
- `WorkOrder_ActualHours_ShouldDefaultToNull` — verify new work orders have no actual hours by default
- `WorkOrder_ActualHours_ShouldRejectNegativeValues` — verify validation rejects negative hour values
- `WorkOrder_ActualHours_ShouldOnlyBeSettableInProgressOrComplete` — verify that setting actual hours in Draft or Assigned status is rejected
- `WorkOrderManage_ShouldDisableActualHoursInput_WhenStatusIsDraft` — bUnit test verifying input is disabled for Draft work orders
- `WorkOrderManage_ShouldEnableActualHoursInput_WhenStatusIsInProgress` — bUnit test verifying input is enabled for InProgress work orders

### Integration Tests
- `WorkOrder_WithActualHours_ShouldPersistAndRetrieve` — save a work order with 6.25 actual hours and verify it round-trips through the database
- `WorkOrder_WithNullActualHours_ShouldPersistAndRetrieve` — save a work order without actual hours and verify null is persisted

### Acceptance Tests
- Navigate to an InProgress work order, enter 2.5 actual hours, save, and verify the value is displayed on the work order detail page
- Navigate to a Draft work order and verify the actual hours input is not editable
