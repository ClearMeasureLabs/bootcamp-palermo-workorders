## 1. Domain Model

- [ ] 1.1 Add `Instructions` property to `WorkOrder` class in `src/Core/Model/WorkOrder.cs` with a private backing field and `getTruncatedString` truncation (matching the `Description` pattern)

## 2. Database Migration

- [ ] 2.1 Create migration script `028_AddInstructionsToWorkOrder.sql` in `src/Database/scripts/Update/` adding `Instructions NVARCHAR(4000) NULL` to `dbo.WorkOrder`

## 3. EF Core Mapping

- [ ] 3.1 Add `entity.Property(e => e.Instructions).HasMaxLength(4000);` to `WorkOrderMap.cs`

## 4. UI — View Model

- [ ] 4.1 Add `Instructions` property (no `[Required]`) to `WorkOrderManageModel` in `src/UI.Shared/Models/WorkOrderManageModel.cs`

## 5. UI — Razor Page

- [ ] 5.1 Add `Instructions` entry to the `Elements` enum in `WorkOrderManage.razor`
- [ ] 5.2 Add `InputTextArea` for Instructions below Description in `WorkOrderManage.razor` with label, `data-testid`, and read-only support

## 6. UI — Code-Behind

- [ ] 6.1 Map `Instructions` from `WorkOrder` to `WorkOrderManageModel` in `CreateViewModel()` in `WorkOrderManage.razor.cs`
- [ ] 6.2 Map `Instructions` from `WorkOrderManageModel` back to `WorkOrder` in `HandleSubmit()` in `WorkOrderManage.razor.cs`

## 7. MCP Server

- [ ] 7.1 Add `instructions` parameter to `CreateWorkOrder` method in `WorkOrderTools.cs`
- [ ] 7.2 Add `Instructions` to `FormatWorkOrderDetail` output in `WorkOrderTools.cs`

## 8. Unit Tests

- [ ] 8.1 Update `PropertiesShouldInitializeToProperDefaults` in `WorkOrderTests.cs` to assert `Instructions` defaults to `""`
- [ ] 8.2 Update `PropertiesShouldGetAndSetValuesProperly` in `WorkOrderTests.cs` to set and assert `Instructions`
- [ ] 8.3 Add `ShouldTruncateTo4000CharactersOnInstructions` test in `WorkOrderTests.cs`

## 9. Integration Tests

- [ ] 9.1 Update `ShouldMapWorkOrderBasicProperties` in `WorkOrderMappingTests.cs` to set and assert `Instructions`
- [ ] 9.2 Update `ShouldSaveWorkOrder` in `WorkOrderMappingTests.cs` to set and assert `Instructions`
