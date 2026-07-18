## Why
Service Level Agreements define maximum response and resolution times. Tracking SLA compliance identifies systemic delays, supports accountability for timely service, and provides management with data to improve operational processes.

## What Changes
- Add `SlaResponseHours` nullable `int` property to the `WorkRequest` domain model in `src/Core/Model/` (maximum hours from creation to assignment)
- Add `SlaResolutionHours` nullable `int` property to the `WorkRequest` domain model in `src/Core/Model/` (maximum hours from creation to completion)
- Add computed SLA status logic to WorkRequest: `GetResponseSlaStatus()` and `GetResolutionSlaStatus()` methods returning OnTrack, AtRisk, or Breached
  - **OnTrack**: Time elapsed is less than 75% of the SLA window
  - **AtRisk**: Time elapsed is between 75% and 100% of the SLA window
  - **Breached**: Time elapsed exceeds the SLA window
- Response SLA is measured from CreatedDate to AssignedDate (or current time if not yet assigned)
- Resolution SLA is measured from CreatedDate to CompletedDate (or current time if not yet completed)
- Update `DataContext` EF Core mapping to persist both SLA columns as nullable int
- Add a new DbUp migration script adding nullable `SlaResponseHours` and `SlaResolutionHours` columns to the `WorkRequest` table
- Update `WorkRequestManage` form to include SLA fields (editable by creator)
- Display SLA status indicators (OnTrack/AtRisk/Breached) with color coding on the `WorkRequestManage` page and `WorkRequestSearch` results

## Capabilities
### New Capabilities
- Users can set SLA response and resolution hour targets when creating or editing a work request
- The system automatically calculates SLA compliance status (OnTrack, AtRisk, Breached) based on elapsed time
- SLA status is displayed with color-coded indicators on the manage page and search results
- Completed work requests show final SLA compliance based on actual dates

### Modified Capabilities
- WorkRequestManage form includes SLA hour input fields and status display
- WorkRequestSearch results display SLA status indicators

## Impact
- **Core** — `WorkRequest` model gains `SlaResponseHours` and `SlaResolutionHours` properties; new computed methods for SLA status calculation
- **DataAccess** — EF Core mapping update for both SLA columns
- **UI.Shared** — `WorkRequestManage` form updated with SLA inputs and status display; `WorkRequestSearch` results updated with SLA indicators
- **Database** — New migration script adding nullable `SlaResponseHours` and `SlaResolutionHours` columns to `WorkRequest` table

## Acceptance Criteria
### Unit Tests
- `WorkRequest_SlaResponseHours_ShouldDefaultToNull` — verify new work requests have no SLA response target by default
- `WorkRequest_SlaResolutionHours_ShouldDefaultToNull` — verify new work requests have no SLA resolution target by default
- `WorkRequest_GetResponseSlaStatus_ShouldReturnOnTrack_WhenUnder75Percent` — verify OnTrack status when elapsed time is under 75% of SLA window
- `WorkRequest_GetResponseSlaStatus_ShouldReturnAtRisk_WhenBetween75And100Percent` — verify AtRisk status when elapsed time is 75-100% of SLA window
- `WorkRequest_GetResponseSlaStatus_ShouldReturnBreached_WhenOver100Percent` — verify Breached status when SLA window is exceeded
- `WorkRequest_GetResolutionSlaStatus_ShouldUseCompletedDate_WhenComplete` — verify completed work requests use CompletedDate for final SLA calculation
- `WorkRequest_GetResponseSlaStatus_ShouldReturnNull_WhenNoSlaSet` — verify null is returned when no SLA target is configured
- `WorkRequestManage_ShouldRenderSlaFields` — bUnit test verifying SLA input fields appear on the form

### Integration Tests
- `WorkRequest_WithSlaHours_ShouldPersistAndRetrieve` — save a work request with SlaResponseHours of 4 and SlaResolutionHours of 24, verify both round-trip through the database
- `WorkRequest_WithNullSlaHours_ShouldPersistAndRetrieve` — save a work request without SLA targets and verify nulls are persisted

### Acceptance Tests
- Navigate to create work request form, set SLA response hours to 4 and SLA resolution hours to 24, save, and verify the SLA values are displayed on the detail page
- View an Assigned work request with an SLA that is within the response window and verify the SLA indicator shows OnTrack (green)
- View a work request that has exceeded its SLA response window and verify the SLA indicator shows Breached (red)
