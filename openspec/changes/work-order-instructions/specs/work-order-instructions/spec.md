## ADDED Requirements

### Requirement: WorkOrder has an Instructions property
The `WorkOrder` entity in `src/Core/Model/WorkOrder.cs` SHALL include an `Instructions` property of type `string?` with a private backing field. The property SHALL default to `""` and SHALL use the existing `getTruncatedString` method to auto-truncate values to 4000 characters, matching the pattern of the `Description` property.

#### Scenario: Instructions defaults to empty string
- **WHEN** a new `WorkOrder` is created
- **THEN** the `Instructions` property SHALL be `""`

#### Scenario: Instructions can be set to a value
- **WHEN** `Instructions` is set to `"Step 1: Turn off power. Step 2: Replace fixture."`
- **THEN** the property SHALL retain that value

#### Scenario: Instructions truncates to 4000 characters
- **WHEN** `Instructions` is set to a string longer than 4000 characters
- **THEN** the property SHALL truncate the value to exactly 4000 characters

### Requirement: Database migration adds Instructions column
A new DbUp migration script `028_AddInstructionsToWorkOrder.sql` SHALL be created in `src/Database/scripts/Update/` that adds an `Instructions` column to the `dbo.WorkOrder` table.

#### Scenario: Migration adds column with default
- **WHEN** the migration script executes
- **THEN** the `dbo.WorkOrder` table SHALL have a new column `Instructions` of type `NVARCHAR(4000)` with a `NOT NULL` constraint and a default value of `''`

### Requirement: EF Core mapping for Instructions
The `WorkOrderMap` class in `src/DataAccess/Mappings/WorkOrderMap.cs` SHALL map the `Instructions` property.

#### Scenario: Instructions is mapped
- **WHEN** the EF Core model is built
- **THEN** the `Instructions` property SHALL be configured with `HasMaxLength(4000)`

### Requirement: UI view model includes Instructions
The `WorkOrderManageModel` class in `src/UI.Shared/Models/WorkOrderManageModel.cs` SHALL include an `Instructions` property of type `string?`. The property SHALL NOT have a `[Required]` attribute since instructions are optional.

#### Scenario: Instructions is displayed on the manage page
- **GIVEN** the `WorkOrderManage.razor` page
- **WHEN** the page renders
- **THEN** there SHALL be an `InputTextArea` for Instructions positioned below the Description field
- **AND** the `InputTextArea` SHALL have `data-testid="@Elements.Instructions"`
- **AND** a speak button with `data-testid="@Elements.SpeakInstructions"` SHALL be adjacent

#### Scenario: Instructions is mapped from domain to view model
- **GIVEN** the `CreateViewModel` method in `WorkOrderManage.razor.cs`
- **WHEN** a `WorkOrder` is loaded
- **THEN** `Instructions` SHALL be mapped from `workOrder.Instructions` to `Model.Instructions`

#### Scenario: Instructions is mapped from view model to domain on save
- **GIVEN** the `HandleSubmit` method in `WorkOrderManage.razor.cs`
- **WHEN** the form is submitted
- **THEN** `workOrder.Instructions` SHALL be set from `Model.Instructions`

### Requirement: MCP server supports Instructions
The `WorkOrderTools` class in `src/McpServer/Tools/WorkOrderTools.cs` SHALL be updated to include Instructions.

#### Scenario: CreateWorkOrder accepts instructions parameter
- **GIVEN** the `CreateWorkOrder` method
- **WHEN** called with an optional `instructions` parameter
- **THEN** the created work order SHALL have its `Instructions` set to the provided value

#### Scenario: FormatWorkOrderDetail includes Instructions
- **GIVEN** the `FormatWorkOrderDetail` method
- **WHEN** formatting a work order
- **THEN** the output SHALL include the `Instructions` property

### Requirement: Unit tests for Instructions in WorkOrderTests
Unit tests SHALL be added/updated in the existing `WorkOrderTests` class in `src/UnitTests/Core/Model/WorkOrderTests.cs`.

#### Scenario: PropertiesShouldInitializeToProperDefaults updated
- **GIVEN** the existing test `PropertiesShouldInitializeToProperDefaults`
- **WHEN** the test is updated to verify `Instructions` default
- **THEN** the test SHALL include `Assert.That(workOrder.Instructions, Is.EqualTo(string.Empty))`

#### Scenario: PropertiesShouldGetAndSetValuesProperly updated
- **GIVEN** the existing test `PropertiesShouldGetAndSetValuesProperly`
- **WHEN** the test is updated to include `Instructions`
- **THEN** the test SHALL set `workOrder.Instructions = "Instructions"` and assert `Assert.That(workOrder.Instructions, Is.EqualTo("Instructions"))`

#### Scenario: ShouldTruncateTo4000CharactersOnInstructions
- **GIVEN** test method `ShouldTruncateTo4000CharactersOnInstructions` exists in `WorkOrderTests.cs`
- **WHEN** `Instructions` is set to a 4001-character string
- **THEN** `Assert.That(order.Instructions.Length, Is.EqualTo(4000))` SHALL pass

### Requirement: Integration tests for Instructions persistence
Integration tests SHALL be updated in `src/IntegrationTests/DataAccess/Mappings/WorkOrderMappingTests.cs`.

#### Scenario: ShouldMapWorkOrderBasicProperties updated
- **GIVEN** the existing test `ShouldMapWorkOrderBasicProperties`
- **WHEN** the test is updated to include `Instructions`
- **THEN** the work order SHALL be created with `Instructions = "Follow safety protocol before starting"`
- **AND** after round-trip, `rehydratedWorkOrder.Instructions.ShouldBe("Follow safety protocol before starting")`

#### Scenario: ShouldSaveWorkOrder updated
- **GIVEN** the existing test `ShouldSaveWorkOrder`
- **WHEN** the test is updated to include `Instructions`
- **THEN** the work order SHALL be created with `Instructions = "baz"`
- **AND** after round-trip, `rehydratedWorkOrder.Instructions.ShouldBe(order.Instructions)`

### Constraints
- The `Instructions` property SHALL be added to the `WorkOrder` class in `src/Core/Model/WorkOrder.cs` (Core project, no new project references)
- Unit tests SHALL be added to the existing `src/UnitTests/Core/Model/WorkOrderTests.cs` file, NOT a new file
- Integration tests SHALL be updated in the existing `src/IntegrationTests/DataAccess/Mappings/WorkOrderMappingTests.cs` file, NOT a new file
- Follow the assertion style already in the target test file: use `Assert.That(x, Is.EqualTo(y))` for unit tests and `.ShouldBe()` for integration tests
- Follow AAA pattern without section comments in tests
- Maintain onion architecture (Core has no project references)
- Use TABS for indentation in the SQL migration script
- The migration script SHALL follow the existing pattern: `BEGIN TRANSACTION`, `PRINT`, `ALTER TABLE`, error check, `COMMIT TRANSACTION`
- Instructions is **optional** — no `[Required]` attribute on the view model
- Instructions does **not** appear on the search results page
