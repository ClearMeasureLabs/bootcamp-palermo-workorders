## MODIFIED Requirements

### Requirement: Unit tests for Instructions property on WorkOrder
Unit tests SHALL be added or updated in `src/UnitTests/Core/Model/WorkOrderTests.cs` to verify the `Instructions` property behavior.

#### Scenario: PropertiesShouldInitializeToProperDefaults updated
- **GIVEN** the existing test `PropertiesShouldInitializeToProperDefaults` in `WorkOrderTests.cs`
- **WHEN** the test is updated to include `Instructions`
- **THEN** the test SHALL assert `Assert.That(workOrder.Instructions, Is.EqualTo(""))`

#### Scenario: PropertiesShouldGetAndSetValuesProperly updated
- **GIVEN** the existing test `PropertiesShouldGetAndSetValuesProperly` in `WorkOrderTests.cs`
- **WHEN** the test is updated to include `Instructions`
- **THEN** the test SHALL set `workOrder.Instructions = "Step 1: Do the thing"` and assert `Assert.That(workOrder.Instructions, Is.EqualTo("Step 1: Do the thing"))`

#### Scenario: ShouldTruncateTo4000CharactersOnInstructions
- **GIVEN** a new test method `ShouldTruncateTo4000CharactersOnInstructions` exists in `WorkOrderTests.cs`
- **WHEN** `workOrder.Instructions` is set to a string of 5000 characters
- **THEN** `workOrder.Instructions.Length` SHALL be 4000
- **AND** the test SHALL follow the same pattern as `ShouldTruncateTo4000CharactersOnDescription`

### Requirement: bUnit tests for Instructions field rendering
bUnit tests SHALL be added in `src/UnitTests/UI.Shared/Pages/WorkOrderManageSpeechTests.cs` to verify the Instructions field and speak button render correctly.

#### Scenario: ShouldRenderInstructionsField
- **GIVEN** test method `ShouldRenderInstructionsField` exists in `WorkOrderManageSpeechTests.cs`
- **AND** the test creates a `Bunit.TestContext`, registers all required stubs, and renders `WorkOrderManage`
- **WHEN** `component.Find($"[data-testid='{WorkOrderManage.Elements.Instructions}']")` is called
- **THEN** the element SHALL be found (not null)

#### Scenario: ShouldRenderSpeakInstructionsButton
- **GIVEN** test method `ShouldRenderSpeakInstructionsButton` exists in `WorkOrderManageSpeechTests.cs`
- **AND** the test creates a `Bunit.TestContext`, registers all required stubs, and renders `WorkOrderManage`
- **WHEN** `component.Find($"[data-testid='{WorkOrderManage.Elements.SpeakInstructions}']")` is called
- **THEN** the element SHALL be found (not null)
- **AND** the element SHALL be a `<button>` with `type="button"`

#### Scenario: SpeakInstructionsButtonShouldInvokeTranslationService
- **GIVEN** test method `SpeakInstructionsButtonShouldInvokeTranslationService` exists in `WorkOrderManageSpeechTests.cs`
- **AND** the stub `IUserSession` returns an `Employee` with `PreferredLanguage = "es-ES"`
- **AND** the stub `ITranslationService` records calls and returns translated text
- **AND** the rendered work order has `Instructions = "Test instructions"`
- **WHEN** the `SpeakInstructions` button's `@onclick` is triggered via `component.Find(...).ClickAsync()`
- **THEN** the stub `ITranslationService.TranslateAsync("Test instructions", "es-ES")` SHALL have been called

### Constraints
- Unit tests SHALL follow existing naming conventions: `ShouldDoSomething` or `PropertyShouldBehavior`
- Tests SHALL use NUnit 4.x with `Assert.That()` and/or Shouldly assertions, matching the existing test file patterns
- bUnit tests SHALL follow the same stub patterns as `WorkOrderManageAttachmentsTests.cs` and `WorkOrderManageSpeechTests.cs`
- Tests SHALL follow AAA pattern without section comments
- bUnit tests SHALL use `using TestContext = Bunit.TestContext;` to avoid conflicts with NUnit's `TestContext`
