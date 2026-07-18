## Why
Seeing the full history of status changes for a work order helps teams understand delays, identify bottlenecks in the workflow, and verify that proper procedures were followed. A visual timeline on the work order detail page provides this context at a glance.

## What Changes
- Add `StatusTransitionTimelineQuery` to `src/Core/Queries/` accepting a work order number and returning an ordered list of `(WorkOrderStatus FromStatus, WorkOrderStatus ToStatus, DateTime TransitionDate, string ChangedBy)`
- Add `StatusTransitionTimelineHandler` in `src/DataAccess/Handlers/` querying audit or history records for status changes
- If no audit table exists, add `WorkOrderStatusHistory` entity to `src/Core/Model/` and corresponding DB migration in `src/Database/scripts/Update/`
- Add `StatusTimeline.razor` component in `src/UI/Client/` rendering a vertical timeline with status labels, dates, and durations between transitions
- Integrate the timeline component into the WorkOrderManage page
- Add API endpoint in `src/UI/Api/`

## Capabilities
### New Capabilities
- Vertical timeline component showing each status transition for a work order
- Each timeline entry displays: previous status, new status, date/time of change, who made the change
- Duration between transitions displayed between entries
- Visual distinction for each status type using color or icons

### Modified Capabilities
- WorkOrderManage page updated to include the status transition timeline section

## Impact
- `src/Core/` — new query class, potentially new `WorkOrderStatusHistory` entity
- `src/DataAccess/` — new handler, potentially new entity configuration and `DbSet`
- `src/Database/` — potential new migration script for status history table
- `src/UI/Client/` — new timeline component, modified WorkOrderManage page
- `src/UI/Api/` — new API endpoint
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `StatusTransitionTimelineHandler` returns transitions in chronological order
- `StatusTransitionTimelineHandler` returns empty list for a work order with no transitions (Draft only)
- Timeline component renders correct number of timeline entries using bUnit
- Duration calculation between transitions is accurate

### Integration Tests
- `StatusTransitionTimelineHandler` retrieves correct transition history from a seeded database
- Status history records persist correctly through EF Core after status changes

### Acceptance Tests
- Navigate to a work order detail page and verify the timeline section is visible
- Transition a work order through multiple statuses and verify each transition appears on the timeline
- Verify transition dates and actor names are displayed correctly
