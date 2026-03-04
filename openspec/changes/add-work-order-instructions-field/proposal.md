## Why

Work order creators currently have only `Title` and `Description` fields. Teams need an additional optional field for detailed execution guidance so assignees can follow specific instructions without overloading the description.

## What Changes

- Add a new optional `Instructions` field to `WorkOrder`
- Persist `Instructions` in the database as `NVARCHAR(4000)`
- Render `Instructions` on the work order manage screen directly under `Description`
- Save and rehydrate `Instructions` in the existing draft/assignment/in-progress/complete flows
- Expose `Instructions` in MCP work order detail output and create-work-order input
- Add/update tests for domain model, mapping, UI behavior, and MCP tools

## Impact

- **Core model**: `WorkOrder` gains `Instructions`
- **Database**: new migration adds `WorkOrder.Instructions`
- **UI**: new form field and model property on `WorkOrderManage`
- **Tooling/API surface**: MCP `create-work-order` accepts optional instructions
- **Tests**: updates in unit, integration, and acceptance coverage where full work order details are asserted
