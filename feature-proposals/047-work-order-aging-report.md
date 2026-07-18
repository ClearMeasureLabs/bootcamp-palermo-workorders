## Why
Work requests that linger too long in a given status indicate process delays or forgotten items. An aging report highlights stale work requests so managers can intervene before small delays become major problems, improving overall service delivery.

## What Changes
- Add `WorkRequestAgingQuery` to `src/Core/Queries/` returning work requests grouped by current status with age (days in current status) calculated from the last status change date or `CreatedDate` for Draft
- Add `WorkRequestAgingHandler` in `src/DataAccess/Handlers/`
- Add `WorkRequestAgingReport.razor` page in `src/UI/Client/` displaying a grouped table with aging data
- Add configurable threshold settings (e.g., warning at 3 days, critical at 7 days) with visual highlighting
- Add API endpoint in `src/UI/Api/`
- Add navigation link in NavMenu under Reports

## Capabilities
### New Capabilities
- Aging report page showing all non-terminal work requests grouped by status
- Each row displays: Work Request Number, Title, Assignee, Days in Current Status
- Color-coded rows: normal (under threshold), warning (approaching threshold), critical (exceeding threshold)
- Configurable thresholds per status

### Modified Capabilities
- None

## Impact
- `src/Core/` — new query class
- `src/DataAccess/` — new handler; may need to track last status change date (see note below)
- `src/UI/Client/` — new report page
- `src/UI/Api/` — new API endpoint
- Potential database impact: if no status change timestamp exists, may need to add a `StatusChangedDate` column or rely on audit/history data
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `WorkRequestAgingHandler` correctly calculates days in current status
- `WorkRequestAgingHandler` groups results by status
- `WorkRequestAgingHandler` excludes completed and cancelled work requests
- Threshold logic correctly classifies work requests as normal, warning, or critical

### Integration Tests
- `WorkRequestAgingHandler` returns accurate aging data from a seeded database with known dates
- Grouping by status produces correct buckets

### Acceptance Tests
- Navigate to the aging report and verify work requests are grouped by status
- Verify a work request created several days ago shows the correct age
- Verify color coding matches the configured thresholds
- Complete a work request and verify it is removed from the report on refresh
