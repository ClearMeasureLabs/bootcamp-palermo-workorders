## ADDED Requirements

### Requirement: WorkOrder has an Instructions property
The `WorkOrder` entity in `src/Core/Model/WorkOrder.cs` SHALL include an `Instructions` property of type `string?` that stores optional detailed instructions for how to perform the work order. The property SHALL default to `""` and SHALL use the existing `getTruncatedString` helper to truncate values exceeding 4000 characters, matching the `Description` property pattern.

#### Scenario: Instructions defaults to empty string
- **WHEN** a new `WorkOrder` is created
- **THEN** the `Instructions` property SHALL be `""`

#### Scenario: Instructions can be set
- **WHEN** `Instructions` is set to `"Step 1: Do this. Step 2: Do that."`
- **THEN** the property SHALL retain the value `"Step 1: Do this. Step 2: Do that."`

#### Scenario: Instructions truncates to 4000 characters
- **WHEN** `Instructions` is set to a string longer than 4000 characters
- **THEN** the property SHALL truncate the value to exactly 4000 characters

#### Scenario: Instructions null is converted to empty string
- **WHEN** `Instructions` is set to `null`
- **THEN** the property SHALL return `""`

### Requirement: Database migration adds Instructions column
A new DbUp migration script `028_AddInstructionsToWorkOrder.sql` SHALL be created in `src/Database/scripts/Update/` that adds an `Instructions` column to the `dbo.WorkOrder` table.

#### Scenario: Migration adds column
- **WHEN** the migration script executes
- **THEN** the `dbo.WorkOrder` table SHALL have a new column `Instructions` of type `NVARCHAR(4000)` that allows NULL values

#### Scenario: Migration follows existing script pattern
- **WHEN** the migration script is reviewed
- **THEN** it SHALL use `BEGIN TRANSACTION`, `PRINT`, `ALTER TABLE`, error handling, and `COMMIT TRANSACTION` with TAB indentation, matching the established migration pattern

### Requirement: EF Core mapping for Instructions
The `WorkOrderMap` class in `src/DataAccess/Mappings/WorkOrderMap.cs` SHALL map the `Instructions` property.

#### Scenario: Instructions is mapped with max length
- **WHEN** the EF Core model is built
- **THEN** the `Instructions` property SHALL be configured with `HasMaxLength(4000)`

### Requirement: Instructions field on the create/edit screen
The Work Order manage page (`src/UI.Shared/Pages/WorkOrderManage.razor`) SHALL display an Instructions field below the Description field.

#### Scenario: Instructions textarea is displayed
- **WHEN** a user navigates to the Work Order create or edit page
- **THEN** an `InputTextArea` for Instructions SHALL be displayed below the Description field and above the Room Number field
- **AND** the field SHALL have a label "Instructions:"
- **AND** the field SHALL have a `data-testid` attribute for test automation
- **AND** the field SHALL be disabled when the form is read-only

#### Scenario: Instructions field is optional
- **WHEN** a user submits the Work Order form with Instructions left empty
- **THEN** the form SHALL submit successfully without validation errors

#### Scenario: Instructions value is persisted on submit
- **GIVEN** a user is on the Work Order create screen
- **WHEN** the user enters text in the Instructions field and clicks Save
- **THEN** the `Instructions` value SHALL be saved to the `WorkOrder` entity and persisted to the database

### Requirement: View model includes Instructions
The `WorkOrderManageModel` in `src/UI.Shared/Models/WorkOrderManageModel.cs` SHALL include an `Instructions` property without a `[Required]` attribute.

#### Scenario: Instructions is mapped from WorkOrder to view model
- **WHEN** `CreateViewModel()` is called in `WorkOrderManage.razor.cs`
- **THEN** the `Instructions` property SHALL be copied from `WorkOrder.Instructions` to `WorkOrderManageModel.Instructions`

#### Scenario: Instructions is mapped from view model to WorkOrder on submit
- **WHEN** `HandleSubmit()` is called in `WorkOrderManage.razor.cs`
- **THEN** the `WorkOrder.Instructions` property SHALL be set from `WorkOrderManageModel.Instructions`

### Requirement: MCP server tools support Instructions
The `WorkOrderTools` class in `src/McpServer/Tools/WorkOrderTools.cs` SHALL be updated to support the Instructions field.

#### Scenario: CreateWorkOrder accepts instructions parameter
- **WHEN** the `CreateWorkOrder` MCP tool is called with an `instructions` parameter
- **THEN** the created `WorkOrder` SHALL have its `Instructions` property set to the provided value

#### Scenario: FormatWorkOrderDetail includes Instructions
- **WHEN** `FormatWorkOrderDetail` formats a work order
- **THEN** the output SHALL include the `Instructions` field value

### Requirement: Unit tests for Instructions property
Unit tests SHALL be added/updated in `src/UnitTests/Core/Model/WorkOrderTests.cs` to cover the Instructions property.

#### Scenario: PropertiesShouldInitializeToProperDefaults includes Instructions
- **GIVEN** the existing test `PropertiesShouldInitializeToProperDefaults`
- **WHEN** the test is updated
- **THEN** it SHALL assert `Assert.That(workOrder.Instructions, Is.EqualTo(""))` 

#### Scenario: PropertiesShouldGetAndSetValuesProperly includes Instructions
- **GIVEN** the existing test `PropertiesShouldGetAndSetValuesProperly`
- **WHEN** the test is updated
- **THEN** it SHALL set `workOrder.Instructions = "Test instructions"` and assert `Assert.That(workOrder.Instructions, Is.EqualTo("Test instructions"))`

#### Scenario: ShouldTruncateTo4000CharactersOnInstructions
- **GIVEN** a new test method `ShouldTruncateTo4000CharactersOnInstructions` in `WorkOrderTests.cs`
- **WHEN** `Instructions` is set to a string of 5000 characters
- **THEN** `workOrder.Instructions.Length` SHALL be `4000`

### Requirement: Integration tests for Instructions persistence
Integration tests SHALL be added/updated in `src/IntegrationTests/DataAccess/Mappings/WorkOrderMappingTests.cs` to verify Instructions survives a database round-trip.

#### Scenario: ShouldMapWorkOrderBasicProperties includes Instructions
- **GIVEN** the existing test `ShouldMapWorkOrderBasicProperties`
- **WHEN** the test is updated to set `Instructions = "Test instructions"`
- **THEN** the rehydrated work order SHALL have `Instructions` equal to `"Test instructions"`

#### Scenario: ShouldSaveWorkOrder includes Instructions
- **GIVEN** the existing test `ShouldSaveWorkOrder`
- **WHEN** the test is updated to set `Instructions = "Detailed instructions for this work order"`
- **THEN** the rehydrated work order SHALL have `Instructions` equal to `"Detailed instructions for this work order"`

### Constraints
- The `Instructions` property SHALL be added to the `WorkOrder` class in `src/Core/Model/WorkOrder.cs` (Core project, no new project references)
- The property SHALL reuse the existing `getTruncatedString` private method for truncation
- Unit tests SHALL be updated in the existing `src/UnitTests/Core/Model/WorkOrderTests.cs` file
- Integration tests SHALL be updated in the existing `src/IntegrationTests/DataAccess/Mappings/WorkOrderMappingTests.cs` file
- The Instructions field SHALL NOT have a `[Required]` attribute on the view model (it is optional)
- The Instructions field SHALL NOT appear on the search results page (it is a detail-level field)
- Follow the assertion style already in the target test files
- Use TABS for indentation in the SQL migration script
- The migration script SHALL follow the existing pattern: `BEGIN TRANSACTION`, `PRINT`, `ALTER TABLE`, error check, `COMMIT TRANSACTION`
- Maintain onion architecture (Core has no project references)
- The `Elements` enum in `WorkOrderManage.razor` SHALL include an `Instructions` entry for test automation
