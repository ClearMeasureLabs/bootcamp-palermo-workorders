## Why
A daily summary gives operations staff a focused snapshot of the day's activity, replacing the need to manually piece together information from multiple views. This improves shift handoffs and ensures nothing from the current day is overlooked.

## What Changes
- Add `DailySummaryQuery` to `src/Core/Queries/` accepting a `DateOnly` parameter and returning: new work orders created, work orders completed, work orders cancelled, status changes made, and top assignees for the day
- Add `DailySummaryHandler` in `src/DataAccess/Handlers/` aggregating work order activity for the specified date
- Add `DailySummary.razor` page in `src/UI/Client/` displaying the summary with cards for key metrics and a list of activity
- Add a date picker to view summaries for previous days
- Add API endpoint in `src/UI/Api/`
- Add navigation link in NavMenu

## Capabilities
### New Capabilities
- Daily summary page defaulting to today's date
- Key metric cards: New Created, Completed, Cancelled, Status Changes
- Activity feed listing individual work order events for the day
- Date picker to navigate to past daily summaries

### Modified Capabilities
- None

## Impact
- `src/Core/` — new query class
- `src/DataAccess/` — new handler with date-filtered aggregation queries
- `src/UI/Client/` — new page with multiple sub-components
- `src/UI/Api/` — new API endpoint
- No database schema changes (uses existing date columns)
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `DailySummaryHandler` returns correct counts for a day with mixed activity
- `DailySummaryHandler` returns zero counts for a day with no activity
- `DailySummaryHandler` only includes activity from the specified date
- `DailySummary` component renders all metric cards using bUnit

### Integration Tests
- `DailySummaryHandler` returns accurate summary from a seeded database with known daily activity
- Date boundary filtering is correct (midnight to midnight)

### Acceptance Tests
- Navigate to the daily summary page and verify today's metrics are displayed
- Create and complete a work order, then verify the summary reflects both actions
- Use the date picker to select a previous date and verify the summary changes accordingly
