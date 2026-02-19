# C4 Architecture: Work Order System domain model

Icons: [Tabler](https://icones.js.org/collection/tabler) via [icones.js.org](https://icones.js.org/). [Register icon pack](https://mermaid.js.org/config/icons.html) to render (e.g. `@iconify-json/tabler`, name `tabler`).

```mermaid
C4Component
  title Work Order System domain model

  Component(entityBase, "EntityBase<T>", "Base class", "Id and equality behavior", "tabler:box")
  Component(listItem, "ListItem", "Base class", "Display/value list abstraction", "tabler:list")
  Component(workOrder, "WorkOrder", "Domain entity", "Aggregate root for work requests", "tabler:clipboard-list")
  Component(employee, "Employee", "Domain entity", "User profile and role membership", "tabler:user")
  Component(role, "Role", "Domain entity", "Authorization role", "tabler:shield")
  Component(workOrderStatus, "WorkOrderStatus", "Value object", "Status code/key/friendly-name", "tabler:circle-dot")

  Rel(workOrder, entityBase, "inherits")
  Rel(employee, entityBase, "inherits")
  Rel(role, entityBase, "inherits")

  Rel(workOrder, workOrderStatus, "status", "1..1 composition")
  Rel(workOrder, employee, "creator", "0..1 association")
  Rel(workOrder, employee, "assignee", "0..1 association")
  Rel(employee, role, "roles", "0..* composition")
```
