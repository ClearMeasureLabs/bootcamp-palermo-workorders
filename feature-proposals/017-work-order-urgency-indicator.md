## Why
Visual urgency indicators on the search page help users quickly scan for time-sensitive items without reading every detail. Color-coded badges and icons provide at-a-glance awareness of which work orders need immediate attention.

## What Changes
- Add CSS-styled urgency badges to `WorkOrderSearch` results based on the Priority field (Low = green, Medium = yellow, High = orange, Critical = red)
- Add a new "Urgency" column to the search results table displaying colored dot or badge indicators
- Add CSS classes for each urgency level with distinct background colors and text styling
- If DueDate is present and the work order is overdue (past due and not Complete/Cancelled), apply an additional "overdue" CSS class with a distinct visual treatment (e.g., red text, strikethrough on due date, or pulsing indicator)
- Add a legend or tooltip explaining the urgency color coding

## Capabilities
### New Capabilities
- Search results display color-coded urgency badges based on priority level
- Overdue work orders receive additional visual highlighting
- A legend explains the color coding to new users

### Modified Capabilities
- WorkOrderSearch results table includes a new Urgency column with visual indicators
- Overdue work orders in the results table receive distinct CSS styling

## Impact
- **UI.Shared** — `WorkOrderSearch` page updated with urgency column, CSS badge styles, and overdue highlighting
- No backend changes required (assumes Priority field already exists from feature 001)

## Acceptance Criteria
### Unit Tests
- `WorkOrderSearch_ShouldRenderUrgencyBadge_ForEachPriority` — bUnit test verifying the correct CSS class is applied for Low, Medium, High, and Critical priorities
- `WorkOrderSearch_ShouldApplyOverdueClass_WhenPastDueDate` — bUnit test verifying overdue CSS class is applied to work orders with past due dates that are not Complete
- `WorkOrderSearch_ShouldNotApplyOverdueClass_WhenComplete` — bUnit test verifying completed work orders do not receive overdue styling even with past due dates
- `WorkOrderSearch_ShouldRenderUrgencyLegend` — bUnit test verifying the color legend is displayed

### Integration Tests
- None required — this feature is purely UI-side with no backend data changes

### Acceptance Tests
- Navigate to work order search with work orders of different priorities and verify each displays the correct color-coded urgency badge
- Create a work order with a past due date and verify it shows overdue visual indicators on the search page
- Verify the urgency legend is visible and explains each color coding
