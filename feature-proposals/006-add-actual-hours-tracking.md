## Why
Recording actual time spent on work requests enables comparison with estimates, identifies efficiency bottlenecks, and supports reporting on labor allocation. This data is essential for improving future estimates and understanding true operational costs.

## What Changes
- Add `ActualHours` nullable `decimal` property to the `WorkRequest` domain model in `src/Core/Model/`
- Update `DataContext` EF Core mapping to persist `ActualHours` as a `decimal(5,2)` column
- Add a new DbUp migration script to `src/Database/scripts/Update/` adding a nullable `ActualHours` column to the `WorkRequest` table
- Update `WorkRequestManage` Blazor form to include a numeric input for ActualHours, editable only when status is InProgress or Complete
- Update `WorkRequestSearch` results to display actual hours column
- Add domain validation: ActualHours can only be set when status is InProgress or Complete

## Capabilities
### New Capabilities
- Users can record actual hours spent on a work request when it is in InProgress or Complete status
- Search results display actual hours for each work request

### Modified Capabilities
- WorkRequestManage form includes a new ActualHours numeric input field with status-based editability
- WorkRequestSearch results table includes an ActualHours column

## Impact
- **Core** — `WorkRequest` model gains nullable `ActualHours` decimal property with status-based validation
- **DataAccess** — EF Core mapping update for `ActualHours` column
- **UI.Shared** — `WorkRequestManage` form updated with conditional numeric input; `WorkRequestSearch` results updated with column
- **Database** — New migration script adding nullable `ActualHours` column to `WorkRequest` table

## Acceptance Criteria
### Unit Tests
- `WorkRequest_ActualHours_ShouldDefaultToNull` — verify new work requests have no actual hours by default
- `WorkRequest_ActualHours_ShouldRejectNegativeValues` — verify validation rejects negative hour values
- `WorkRequest_ActualHours_ShouldOnlyBeSettableInProgressOrComplete` — verify that setting actual hours in Draft or Assigned status is rejected
- `WorkRequestManage_ShouldDisableActualHoursInput_WhenStatusIsDraft` — bUnit test verifying input is disabled for Draft work requests
- `WorkRequestManage_ShouldEnableActualHoursInput_WhenStatusIsInProgress` — bUnit test verifying input is enabled for InProgress work requests

### Integration Tests
- `WorkRequest_WithActualHours_ShouldPersistAndRetrieve` — save a work request with 6.25 actual hours and verify it round-trips through the database
- `WorkRequest_WithNullActualHours_ShouldPersistAndRetrieve` — save a work request without actual hours and verify null is persisted

### Acceptance Tests
- Navigate to an InProgress work request, enter 2.5 actual hours, save, and verify the value is displayed on the work request detail page
- Navigate to a Draft work request and verify the actual hours input is not editable
