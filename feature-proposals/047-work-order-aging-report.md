## Why
Work orders that linger too long in a given status indicate process delays or forgotten items. An aging report highlights stale work orders so managers can intervene before small delays become major problems, improving overall service delivery.

## What Changes
- Add `WorkOrderAgingQuery` to `src/Core/Queries/` returning work orders grouped by current status with age (days in current status) calculated from the last status change date or `CreatedDate` for Draft
- Add `WorkOrderAgingHandler` in `src/DataAccess/Handlers/`
- Add `WorkOrderAgingReport.razor` page in `src/UI/Client/` displaying a grouped table with aging data
- Add configurable threshold settings (e.g., warning at 3 days, critical at 7 days) with visual highlighting
- Add API endpoint in `src/UI/Api/`
- Add navigation link in NavMenu under Reports

## Capabilities
### New Capabilities
- Aging report page showing all non-terminal work orders grouped by status
- Each row displays: Work Order Number, Title, Assignee, Days in Current Status
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
- `WorkOrderAgingHandler` correctly calculates days in current status
- `WorkOrderAgingHandler` groups results by status
- `WorkOrderAgingHandler` excludes completed and cancelled work orders
- Threshold logic correctly classifies work orders as normal, warning, or critical

### Integration Tests
- `WorkOrderAgingHandler` returns accurate aging data from a seeded database with known dates
- Grouping by status produces correct buckets

### Acceptance Tests
- Navigate to the aging report and verify work orders are grouped by status
- Verify a work order created several days ago shows the correct age
- Verify color coding matches the configured thresholds
- Complete a work order and verify it is removed from the report on refresh
