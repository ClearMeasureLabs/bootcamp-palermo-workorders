## 1. Domain Model

- [ ] 1.1 Add `Instructions` property to `WorkOrder` entity in `src/Core/Model/WorkOrder.cs` with private backing field, auto-truncation to 4000 characters (matching `Description` pattern), and default value of `""`
- [ ] 1.2 Add unit tests for `Instructions` in `src/UnitTests/Core/Model/WorkOrderTests.cs`

## 2. Database Migration

- [ ] 2.1 Create `028_AddInstructionsToWorkOrder.sql` in `src/Database/scripts/Update/` adding a nullable `NVARCHAR(4000)` column `Instructions` to `dbo.WorkOrder`

## 3. EF Core Mapping

- [ ] 3.1 Update `WorkOrderMap` in `src/DataAccess/Mappings/WorkOrderMap.cs` to map `Instructions` with `HasMaxLength(4000)`

## 4. View Model

- [ ] 4.1 Add `Instructions` property to `WorkOrderManageModel` in `src/UI.Shared/Models/WorkOrderManageModel.cs`

## 5. Work Order Manage UI

- [ ] 5.1 Add Instructions `InputTextArea` field to `WorkOrderManage.razor` below Description, with `data-testid` and a megaphone/speak button
- [ ] 5.2 Add `Instructions` and `SpeakInstructions` entries to the `Elements` enum in `WorkOrderManage.razor`
- [ ] 5.3 Add `SpeakInstructionsAsync()` method to `WorkOrderManage.razor.cs`
- [ ] 5.4 Map `Instructions` in `CreateViewModel()` and `HandleSubmit()` in `WorkOrderManage.razor.cs`

## 6. MCP Server Update

- [ ] 6.1 Update `FormatWorkOrderDetail` in `src/McpServer/Tools/WorkOrderTools.cs` to include `Instructions`
- [ ] 6.2 Update `create-work-order` tool to accept optional `instructions` parameter
- [ ] 6.3 Update `FormatWorkOrderSummary` to include `Instructions` (truncated) if present

## 7. LLM Gateway Update

- [ ] 7.1 Update `WorkOrderTool.GetWorkOrderByNumber` in `src/LlmGateway/WorkOrderTool.cs` to include `Instructions` in the returned data

## 8. Unit Tests

- [ ] 8.1 Add `InstructionsShouldDefaultToEmptyString` test in `WorkOrderTests.cs`
- [ ] 8.2 Add `ShouldGetAndSetInstructions` test in `WorkOrderTests.cs`
- [ ] 8.3 Add `ShouldTruncateInstructionsTo4000Characters` test in `WorkOrderTests.cs`
- [ ] 8.4 Update `PropertiesShouldInitializeToProperDefaults` to assert `Instructions == ""`
- [ ] 8.5 Update `PropertiesShouldGetAndSetValuesProperly` to include `Instructions`
- [ ] 8.6 Add bUnit test `ShouldRenderInstructionsField` in `src/UnitTests/UI.Shared/Pages/WorkOrderManageSpeechTests.cs`
- [ ] 8.7 Add bUnit test `ShouldRenderSpeakInstructionsButton` in `WorkOrderManageSpeechTests.cs`

## 9. Integration Tests

- [ ] 9.1 Add `ShouldPersistInstructions` test in `src/IntegrationTests/DataAccess/WorkOrderQueryHandlerTests.cs`
- [ ] 9.2 Add `ShouldPersistNullInstructionsAsDefault` test verifying null Instructions round-trips correctly

## 10. Acceptance Tests

- [ ] 10.1 Update `CreateAndSaveNewWorkOrder` helper in `AcceptanceTestBase.cs` to fill in Instructions
- [ ] 10.2 Update `ShouldCreateNewWorkOrderAndVerifyOnSearchScreen` test in `WorkOrderSaveDraftTests.cs` to verify Instructions
- [ ] 10.3 Update `ShouldAssignEmployeeAndSave` test to verify Instructions persists through edit

## 11. Verification

- [ ] 11.1 Verify solution builds: `dotnet build src/ChurchBulletin.sln`
- [ ] 11.2 Verify unit tests pass: `dotnet test src/UnitTests/UnitTests.csproj`
- [ ] 11.3 Run full private build: `pwsh -NoProfile -ExecutionPolicy Bypass -File ./PrivateBuild.ps1`
