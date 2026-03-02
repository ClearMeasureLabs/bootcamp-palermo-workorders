# C4 Architecture: Work Order System domain model

Icons: [Tabler](https://icones.js.org/collection/tabler) via [icones.js.org](https://icones.js.org/). [Register icon pack](https://mermaid.js.org/config/icons.html) to render (e.g. `@iconify-json/tabler`, name `tabler`).

```mermaid
C4Component
  title Work Order System domain model

  Component(entityBase, "EntityBase<T>", "Abstract base class", "Id : Guid, equality by Id", "tabler:box")
  Component(workOrder, "WorkOrder", "Domain entity", "Aggregate root for work requests", "tabler:clipboard-list")
  Component(employee, "Employee", "Domain entity", "User profile and role membership", "tabler:user")
  Component(role, "Role", "Domain entity", "Authorization role with create/fulfill permissions", "tabler:shield")
  Component(workOrderStatus, "WorkOrderStatus", "Value object", "Smart enum: Draft, Assigned, InProgress, Complete", "tabler:circle-dot")
  Component(stateCommandBase, "StateCommandBase", "Abstract record", "Base for all state transition commands", "tabler:arrow-right")
  Component(saveDraft, "SaveDraftCommand", "State command", "Draft -> Draft (save)", "tabler:device-floppy")
  Component(draftToAssigned, "DraftToAssignedCommand", "State command", "Draft -> Assigned", "tabler:user-check")
  Component(assignedToInProgress, "AssignedToInProgressCommand", "State command", "Assigned -> InProgress", "tabler:player-play")
  Component(inProgressToComplete, "InProgressToCompleteCommand", "State command", "InProgress -> Complete", "tabler:circle-check")
  Component(stateCommandResult, "StateCommandResult", "Record", "Result of a state command execution", "tabler:clipboard-check")

  Rel(workOrder, entityBase, "inherits")
  Rel(employee, entityBase, "inherits")
  Rel(role, entityBase, "inherits")

  Rel(workOrder, workOrderStatus, "status", "1..1 composition")
  Rel(workOrder, employee, "creator", "0..1 association")
  Rel(workOrder, employee, "assignee", "0..1 association")
  Rel(employee, role, "roles", "0..* composition")

  Rel(saveDraft, stateCommandBase, "extends")
  Rel(draftToAssigned, stateCommandBase, "extends")
  Rel(assignedToInProgress, stateCommandBase, "extends")
  Rel(inProgressToComplete, stateCommandBase, "extends")
  Rel(stateCommandBase, workOrder, "operates on")
  Rel(stateCommandBase, employee, "CurrentUser")
  Rel(stateCommandBase, workOrderStatus, "begin/end status")
  Rel(stateCommandResult, workOrder, "contains")
```


