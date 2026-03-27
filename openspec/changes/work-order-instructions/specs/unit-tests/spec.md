## ADDED Requirements

### Requirement: Unit tests for Instructions property in WorkOrderTests
Unit tests SHALL be added to the existing `WorkOrderTests` class in `src/UnitTests/Core/Model/WorkOrderTests.cs`. Tests SHALL follow the existing naming conventions and assertion patterns already present in that file.

#### Scenario: InstructionsShouldDefaultToNull
- **GIVEN** test method `InstructionsShouldDefaultToNull` exists in `src/UnitTests/Core/Model/WorkOrderTests.cs`
- **WHEN** a new `WorkOrder()` is created with the default constructor
- **THEN** `Assert.That(workOrder.Instructions, Is.EqualTo(null))` SHALL pass

#### Scenario: InstructionsShouldGetAndSetProperly
- **GIVEN** test method `InstructionsShouldGetAndSetProperly` exists in `src/UnitTests/Core/Model/WorkOrderTests.cs`
- **WHEN** `workOrder.Instructions` is set to `"Detailed instructions here"`
- **THEN** `Assert.That(workOrder.Instructions, Is.EqualTo("Detailed instructions here"))` SHALL pass

#### Scenario: ShouldTruncateTo4000CharactersOnInstructions
- **GIVEN** test method `ShouldTruncateTo4000CharactersOnInstructions` exists in `src/UnitTests/Core/Model/WorkOrderTests.cs`
- **AND** a string of 4001 characters is created
- **WHEN** `workOrder.Instructions` is set to that string
- **THEN** `Assert.That(workOrder.Instructions.Length, Is.EqualTo(4000))` SHALL pass

#### Scenario: PropertiesShouldInitializeToProperDefaults updated
- **GIVEN** the existing test `PropertiesShouldInitializeToProperDefaults` in `src/UnitTests/Core/Model/WorkOrderTests.cs`
- **WHEN** the test is updated to verify `Instructions` default
- **THEN** the test SHALL include `Assert.That(workOrder.Instructions, Is.EqualTo(null))`

#### Scenario: PropertiesShouldGetAndSetValuesProperly updated
- **GIVEN** the existing test `PropertiesShouldGetAndSetValuesProperly` in `src/UnitTests/Core/Model/WorkOrderTests.cs`
- **WHEN** the test is updated to include `Instructions`
- **THEN** the test SHALL set `workOrder.Instructions = "Instructions"` and assert `Assert.That(workOrder.Instructions, Is.EqualTo("Instructions"))`

### Requirement: BogusOverrides updated for Instructions
The `BogusOverrides` class in `src/UnitTests/BogusOverrides.cs` SHALL clamp the auto-generated `Instructions` property to a valid length.

#### Scenario: BogusOverrides clamps Instructions
- **WHEN** the `BogusOverrides.Generate()` method is examined
- **THEN** the `WorkOrder` case SHALL include `order.Instructions = order.Instructions?.ClampLength(1, 4000);`
- **AND** the null-conditional operator `?.` SHALL be used because Instructions defaults to null

### Constraints
- Unit tests SHALL be added to the existing `src/UnitTests/Core/Model/WorkOrderTests.cs` file, NOT a new file
- BogusOverrides changes SHALL be in the existing `src/UnitTests/BogusOverrides.cs` file
- Follow AAA pattern without section comments in tests
- Use `Assert.That(x, Is.EqualTo(y))` for consistency with existing tests in `WorkOrderTests.cs`
- Follow the existing naming convention (e.g., `ShouldDoSomething`, `PropertyShouldInitializeProperly`)
