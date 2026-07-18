## Why
Facility managers and building occupants need a quick overview of maintenance activity without logging in. A public status page provides transparency into work order throughput, highlights bottlenecks by status category, and builds confidence that submitted requests are being handled.

## What Changes
- Add `WorkOrderStatisticsQuery` in `src/Core/Queries/` returning: counts by status, count of completions in last 7 days, average hours from creation to completion
- Add `WorkOrderStatisticsResult` record in `src/Core/Queries/` with StatusCounts dictionary, RecentCompletionCount, and AverageResponseTimeHours properties
- Add `WorkOrderStatisticsHandler` in `src/DataAccess/Handlers/` querying aggregated data from `DataContext`
- Add `StatusPage.razor` component in `src/UI/Client/Pages/` with route `/status`
- Add `StatusPageController` in `src/UI/Api/` serving statistics as JSON at `GET /api/status`
- Exclude `/status` and `/api/status` routes from authentication middleware
- Display status counts as cards, recent completions as a number, and average response time in hours

## Capabilities
### New Capabilities
- Public-facing `/status` page accessible without authentication
- Work order count cards broken down by each WorkOrderStatus value
- Recent completion count for the last 7 days
- Average response time (creation to completion) displayed in hours
- JSON API endpoint at `/api/status` for programmatic consumption

### Modified Capabilities
- None

## Impact
- **src/Core/Queries/** - New `WorkOrderStatisticsQuery` and `WorkOrderStatisticsResult`
- **src/DataAccess/Handlers/** - New `WorkOrderStatisticsHandler`
- **src/UI/Client/Pages/** - New `StatusPage.razor` component
- **src/UI/Api/** - New `StatusPageController`
- **src/UI/Server/** - Authentication exclusion for status routes
- **Dependencies** - No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `StatusPage_Rendering_ShowsAllStatusCategories` - bUnit render verifies cards for Draft, Assigned, InProgress, Complete, Cancelled
- `WorkOrderStatistics_WithMixedStatuses_ReturnsCorrectCounts` - Query with 3 Draft and 2 Assigned returns {Draft:3, Assigned:2}
- `WorkOrderStatistics_NoWorkOrders_ReturnsZeroCounts` - Empty database returns all counts as zero
- `WorkOrderStatistics_AverageResponseTime_CalculatesCorrectly` - Two completed work orders with 10h and 20h response times return 15h average
- `WorkOrderStatistics_RecentCompletions_OnlyCountsLast7Days` - Completions older than 7 days excluded from RecentCompletionCount

### Integration Tests
- `StatusStatistics_PersistedRecords_MatchQueryResults` - Seed work orders in database, execute statistics query, verify counts match seeded data
- `StatusApi_ReturnsJsonWithCorrectStructure` - Call `/api/status` endpoint, verify JSON response contains statusCounts, recentCompletionCount, and averageResponseTimeHours

### Acceptance Tests
- `PublicStatusPage_NoLogin_PageLoads` - Navigate to `/status` without logging in, verify page renders with status cards
- `PublicStatusPage_DisplaysAccurateCounts` - Seed known data, navigate to `/status`, verify displayed counts match expected values
- `PublicStatusPage_CompletionStats_Visible` - Navigate to `/status`, verify recent completion count and average response time sections are visible
