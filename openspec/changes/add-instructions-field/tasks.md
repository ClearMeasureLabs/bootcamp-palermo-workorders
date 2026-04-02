## 1. Domain Model

- [ ] 1.1 Add `Instructions` nullable string property to `WorkOrder` class in `src/Core/Model/WorkOrder.cs` (max 4000 chars, no auto-uppercase)

## 2. Database Migration

- [ ] 2.1 Create DbUp script `028_AddInstructionsToWorkOrder.sql` in `src/Database/scripts/Update/` adding `Instructions` nvarchar(4000) NULL column to `[dbo].[WorkOrder]`

## 3. EF Core Mapping

- [ ] 3.1 Add `Instructions` property mapping in `src/DataAccess/Mappings/WorkOrderMap.cs` with HasMaxLength(4000)

## 4. UI Updates

- [ ] 4.1 Add `Instructions` property to the work order view model
- [ ] 4.2 Add Instructions text area field to `WorkOrderManage.razor` between Description and Room fields, with data-testid, read-only support, and form label
- [ ] 4.3 Add `Instructions` enum value to the `Elements` enum for test ID

## 5. Tests

- [ ] 5.1 Add unit test verifying Instructions property get/set on WorkOrder
- [ ] 5.2 Add unit test verifying Instructions is not auto-uppercased
