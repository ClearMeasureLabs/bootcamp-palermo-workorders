## Why

Work orders currently have Title, Description, RoomNumber, and status-related fields but lack a dedicated field for instructions. When a work order is assigned, the assignee needs specific instructions on how to complete the task. Currently this information is crammed into the Description field, conflating what the work order *is* with how it should be *done*. A separate Instructions field provides a clear place for actionable guidance.

## What Changes

- New `Instructions` property on the `WorkOrder` domain model (string, max 4000 characters, nullable)
- New database column `Instructions` on the `[dbo].[WorkOrder]` table via a DbUp migration script
- Updated EF Core mapping in `WorkOrderMap` to include the Instructions column
- Updated Blazor UI form (`WorkOrderManage.razor`) with an Instructions text area field
- Updated view model to include Instructions for data binding
- Unit tests for the new property

## Capabilities

### New Capabilities
- `work-order-instructions`: Users can enter, view, and edit instructions on a work order through the UI

### Modified Capabilities
- Work order create/edit form includes a new Instructions field between Description and Room

## Impact

- **Domain model**: New nullable string property on `WorkOrder`
- **Database**: New migration script adding `Instructions` column (nvarchar(4000), nullable)
- **EF mapping**: Updated `WorkOrderMap` with Instructions property configuration
- **UI**: Updated `WorkOrderManage.razor` with Instructions text area
- **Tests**: New unit tests for the Instructions property
