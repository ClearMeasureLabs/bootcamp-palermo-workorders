## Why
Recognizing top-performing employees motivates the team and promotes healthy competition. A leaderboard showing who has completed the most work orders in the current month gives visible credit to high achievers and helps managers identify reliable assignees.

## What Changes
- Add `LeaderboardQuery` to `src/Core/Queries/` returning a ranked list of `(Employee Assignee, int CompletedCount, TimeSpan AverageCompletionTime)` for the current month
- Add `LeaderboardHandler` in `src/DataAccess/Handlers/` filtering completed work orders by current month and grouping by assignee
- Add `TopPerformers.razor` component in `src/UI/Client/` displaying a ranked list or podium-style layout
- Add API endpoint in `src/UI/Api/`
- Display the leaderboard on the dashboard page

## Capabilities
### New Capabilities
- Leaderboard component showing top employees ranked by completed work orders in the current calendar month
- Display: Rank, Employee Name, Completed Count, Average Completion Time
- Top 3 highlighted with distinct styling
- Optional month selector to view previous months

### Modified Capabilities
- Dashboard page updated to include the leaderboard

## Impact
- `src/Core/` — new query class
- `src/DataAccess/` — new handler with EF Core aggregation and ordering
- `src/UI/Client/` — new leaderboard component
- `src/UI/Api/` — new API endpoint
- No database schema changes required
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `LeaderboardHandler` ranks employees by completed count in descending order
- `LeaderboardHandler` only counts work orders completed in the current month
- `LeaderboardHandler` calculates average completion time correctly per employee
- `LeaderboardHandler` returns empty list when no work orders completed in the current month
- `TopPerformers` component renders ranked entries using bUnit

### Integration Tests
- `LeaderboardHandler` returns correctly ranked results from a seeded database with multiple assignees completing work in the current month
- Month boundary filtering is accurate

### Acceptance Tests
- Navigate to the dashboard and verify the leaderboard is visible
- Complete a work order and verify the assignee appears or moves up on the leaderboard on refresh
- Verify the top 3 entries have distinct visual styling
