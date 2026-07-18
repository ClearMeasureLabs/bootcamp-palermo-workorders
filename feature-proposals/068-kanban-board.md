## Why
A Kanban board provides an intuitive visual representation of work order workflow across statuses. Drag-and-drop status transitions reduce clicks and make it easy for managers and assignees to see the overall state of work at a glance.

## What Changes
- Add `KanbanBoard.razor` page in `src/UI/Client/Pages/` with route `/kanban` displaying columns for Draft, Assigned, InProgress, and Complete statuses
- Add `KanbanColumn.razor` component in `src/UI.Shared/Components/` rendering a vertical list of work order cards within a status column
- Add `KanbanCard.razor` component in `src/UI.Shared/Components/` displaying a compact work order card (number, title, assignee) within a column
- Add JavaScript interop for HTML5 drag-and-drop API in `src/UI/Client/wwwroot/js/kanban-drag.js` handling dragstart, dragover, and drop events
- Add `MoveWorkOrderCommand` in `src/Core/Model/StateCommands/` that validates and executes the status transition when a card is dropped in a new column
- Add handler for `MoveWorkOrderCommand` in `src/DataAccess/Handlers/`
- Add `GetWorkOrdersByStatusQuery` in `src/Core/Queries/` returning work orders grouped by status
- Add handler for `GetWorkOrdersByStatusQuery` in `src/DataAccess/Handlers/`
- Add navigation link to Kanban board in `NavMenu.razor`
- Add CSS styles for Kanban layout (horizontal columns, card spacing, drop zone highlighting)

## Capabilities
### New Capabilities
- Kanban board page with columns for each active status (Draft, Assigned, InProgress, Complete)
- Drag-and-drop work order cards between columns to trigger status transitions
- Visual drop zone highlighting when dragging over a valid target column
- Invalid transitions prevented (card snaps back with error toast if transition is not allowed)
- Real-time column counts showing number of work orders per status

### Modified Capabilities
- None

## Impact
- **Core**: New `MoveWorkOrderCommand`, new `GetWorkOrdersByStatusQuery`
- **DataAccess**: New handlers for move command and grouped status query
- **UI.Client**: New `KanbanBoard.razor` page, new JS interop file
- **UI.Shared**: New `KanbanColumn.razor` and `KanbanCard.razor` components
- **Database**: No schema changes required
- **Dependencies**: No new NuGet packages required (uses HTML5 drag-and-drop API)

## Acceptance Criteria
### Unit Tests
- `MoveWorkOrderCommand_FromDraftToAssigned_Succeeds` - command executes valid transition
- `MoveWorkOrderCommand_FromCompleteToAssigned_Fails` - command rejects invalid transition
- `GetWorkOrdersByStatusQuery_Handler_GroupsCorrectly` - handler returns work orders grouped by status with correct counts
- `KanbanColumn_RendersCards_ForGivenStatus` - bUnit test confirming column shows cards matching the column status
- `KanbanCard_RendersNumber_AndTitle` - bUnit test confirming card shows work order number and title

### Integration Tests
- `GetWorkOrdersByStatusQuery_ReturnsAllStatuses` - query returns groups for all active statuses
- `MoveWorkOrderCommand_PersistsStatusChange` - work order status updates in the database after move

### Acceptance Tests
- Navigate to `/kanban` and verify four columns render with `data-testid="kanban-column-draft"`, `data-testid="kanban-column-assigned"`, `data-testid="kanban-column-inprogress"`, `data-testid="kanban-column-complete"`
- Drag a card with `data-testid="kanban-card"` from the Draft column to the Assigned column and verify the card moves and the status updates
- Attempt to drag a card to an invalid column and verify it returns to its original position
- Verify each column header displays the count of work orders with `data-testid="kanban-column-count"`
