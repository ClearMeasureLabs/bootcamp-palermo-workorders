# C4 Architecture: Work Request System domain model

Icons: [Tabler](https://icones.js.org/collection/tabler) via [icones.js.org](https://icones.js.org/). [Register icon pack](https://mermaid.js.org/config/icons.html) to render (e.g. `@iconify-json/tabler`, name `tabler`).

```mermaid
C4Component
  title Work Request System domain model

  Component(entityBase, "EntityBase<T>", "Abstract base class", "Id : Guid, equality by Id", "tabler:box")
  Component(workRequest, "WorkRequest", "Domain entity", "Aggregate root for work requests", "tabler:clipboard-list")
  Component(employee, "Employee", "Domain entity", "User profile and role membership", "tabler:user")
  Component(role, "Role", "Domain entity", "Authorization role with create/fulfill permissions", "tabler:shield")
  Component(workRequestStatus, "WorkRequestStatus", "Value object", "Smart enum: Draft, Assigned, InProgress, Complete", "tabler:circle-dot")
  Component(stateCommandBase, "StateCommandBase", "Abstract record", "Base for all state transition commands", "tabler:arrow-right")
  Component(saveDraft, "SaveDraftCommand", "State command", "Draft -> Draft (save)", "tabler:device-floppy")
  Component(draftToAssigned, "DraftToAssignedCommand", "State command", "Draft -> Assigned", "tabler:user-check")
  Component(assignedToInProgress, "AssignedToInProgressCommand", "State command", "Assigned -> InProgress", "tabler:player-play")
  Component(inProgressToComplete, "InProgressToCompleteCommand", "State command", "InProgress -> Complete", "tabler:circle-check")
  Component(stateCommandResult, "StateCommandResult", "Record", "Result of a state command execution", "tabler:clipboard-check")

  Rel(workRequest, entityBase, "inherits")
  Rel(employee, entityBase, "inherits")
  Rel(role, entityBase, "inherits")

  Rel(workRequest, workRequestStatus, "status", "1..1 composition")
  Rel(workRequest, employee, "creator", "0..1 association")
  Rel(workRequest, employee, "assignee", "0..1 association")
  Rel(employee, role, "roles", "0..* composition")

  Rel(saveDraft, stateCommandBase, "extends")
  Rel(draftToAssigned, stateCommandBase, "extends")
  Rel(assignedToInProgress, stateCommandBase, "extends")
  Rel(inProgressToComplete, stateCommandBase, "extends")
  Rel(stateCommandBase, workRequest, "operates on")
  Rel(stateCommandBase, employee, "CurrentUser")
  Rel(stateCommandBase, workRequestStatus, "begin/end status")
  Rel(stateCommandResult, workRequest, "contains")
```


