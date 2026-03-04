## Why
Complex work orders may have multiple discrete steps. Subtasks let assignees break work into trackable pieces and show completion progress, providing more granular visibility into how much of a work order is done.

## What Changes
- Add `WorkOrderSubtask` entity to `src/Core/Model/` with properties: Id (Guid), WorkOrderId (Guid), Title (string), IsCompleted (bool), SortOrder (int)
- Add navigation property `Subtasks` (ICollection<WorkOrderSubtask>) to `WorkOrder` domain model
- Add `AddSubtaskCommand` to `src/Core/Model/StateCommands/` containing WorkOrderId, Title, and SortOrder
- Add `ToggleSubtaskCommand` to `src/Core/Model/StateCommands/` containing SubtaskId to toggle IsCompleted
- Add `RemoveSubtaskCommand` to `src/Core/Model/StateCommands/` containing SubtaskId
- Add EF Core mapping for `WorkOrderSubtask` in DataAccess
- Add handlers for subtask commands in `src/DataAccess/Handlers/`
- Add a new DbUp migration script creating the `WorkOrderSubtask` table with FK to `WorkOrder`
- Add a subtask checklist component on the `WorkOrderManage` page with add, toggle, and remove functionality

## Capabilities
### New Capabilities
- Users can add subtasks with a title to any work order that is not Complete or Cancelled
- Users can mark subtasks as completed or uncompleted via checkbox toggle
- Users can remove subtasks from a work order
- Subtasks display in sort order with completion status
- A progress indicator shows how many subtasks are completed out of total

### Modified Capabilities
- WorkOrderManage page includes a new subtask checklist section

## Impact
- **Core** — New `WorkOrderSubtask` entity; new `AddSubtaskCommand`, `ToggleSubtaskCommand`, `RemoveSubtaskCommand`
- **DataAccess** — EF Core mapping for `WorkOrderSubtask`; new MediatR handlers for subtask operations
- **UI.Shared** — Subtask checklist component on `WorkOrderManage` page
- **Database** — New migration script creating `WorkOrderSubtask` table

## Acceptance Criteria
### Unit Tests
- `WorkOrderSubtask_ShouldRequireTitle` — verify a subtask with empty title is rejected
- `WorkOrderSubtask_ShouldDefaultToNotCompleted` — verify new subtasks start as not completed
- `AddSubtaskCommand_ShouldAddSubtaskToWorkOrder` — verify command adds subtask to the work order
- `ToggleSubtaskCommand_ShouldFlipIsCompleted` — verify toggling changes false to true and true to false
- `RemoveSubtaskCommand_ShouldRemoveSubtask` — verify command removes the subtask
- `WorkOrderManage_ShouldRenderSubtaskChecklist` — bUnit test verifying checklist renders with existing subtasks

### Integration Tests
- `AddSubtaskCommand_ShouldPersistSubtask` — add a subtask and verify it persists in the database
- `ToggleSubtaskCommand_ShouldPersistCompletionState` — toggle a subtask and verify the IsCompleted flag is updated in the database
- `RemoveSubtaskCommand_ShouldDeleteSubtask` — remove a subtask and verify it is deleted from the database

### Acceptance Tests
- Navigate to an existing work order, add a subtask titled "Replace light fixture", and verify it appears in the checklist
- Click the checkbox on a subtask to mark it complete and verify the visual state updates
- Add three subtasks, complete two, and verify the progress indicator shows "2 of 3 complete"
