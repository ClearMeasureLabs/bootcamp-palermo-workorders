## ADDED Requirements

### Requirement: Employee has a PreferredLanguage property
The `Employee` entity in `src/Core/Model/Employee.cs` SHALL include a `PreferredLanguage` property of type `string` that stores a BCP 47 language tag (e.g., `"en-US"`, `"es-ES"`, `"de-DE"`, `"fr-FR"`). The property SHALL default to `"en-US"` and SHALL NOT be nullable.

#### Scenario: PreferredLanguage defaults to en-US
- **WHEN** a new `Employee` is created using the parameterless constructor
- **THEN** the `PreferredLanguage` property SHALL be `"en-US"`

#### Scenario: PreferredLanguage defaults to en-US with parameterized constructor
- **WHEN** a new `Employee` is created using the four-parameter constructor (`userName`, `firstName`, `lastName`, `emailAddress`)
- **THEN** the `PreferredLanguage` property SHALL be `"en-US"`

#### Scenario: PreferredLanguage can be set to a supported language
- **WHEN** `PreferredLanguage` is set to `"es-ES"`
- **THEN** the property SHALL retain the value `"es-ES"`

### Requirement: Database migration adds PreferredLanguage column
A new DbUp migration script `026_AddPreferredLanguageToEmployee.sql` SHALL be created in `src/Database/scripts/Update/` that adds a `PreferredLanguage` column to the `dbo.Employee` table.

#### Scenario: Migration adds column with default
- **WHEN** the migration script executes
- **THEN** the `dbo.Employee` table SHALL have a new column `PreferredLanguage` of type `NVARCHAR(10)` with a `NOT NULL` constraint and a default value of `'en-US'`

#### Scenario: Migration is idempotent
- **WHEN** the migration script is applied to a database that already has the column
- **THEN** the script SHALL not fail (use `IF NOT EXISTS` guard or rely on DbUp's journal table)

### Requirement: EF Core mapping for PreferredLanguage
The `EmployeeMap` class in `src/DataAccess/Mappings/EmployeeMap.cs` SHALL map the `PreferredLanguage` property.

#### Scenario: PreferredLanguage is mapped
- **WHEN** the EF Core model is built
- **THEN** the `PreferredLanguage` property SHALL be configured as `IsRequired()` with `HasMaxLength(10)` and `HasDefaultValue("en-US")`

### Requirement: Unit tests for PreferredLanguage in EmployeeTests
Unit tests SHALL be added to the existing `EmployeeTests` class in `src/UnitTests/Core/Model/EmployeeTests.cs`. Tests SHALL follow the existing naming convention (e.g., `ShouldDoSomething`, `PropertyShouldInitializeProperly`) and use the same assertion patterns already present in that file.

#### Scenario: PreferredLanguageDefaultsToEnUS
- **GIVEN** test method `PreferredLanguageShouldDefaultToEnUS` exists in `src/UnitTests/Core/Model/EmployeeTests.cs`
- **WHEN** a new `Employee()` is created with the parameterless constructor
- **THEN** `Assert.That(employee.PreferredLanguage, Is.EqualTo("en-US"))` SHALL pass
- **AND** the test SHALL follow the same style as `PropertiesShouldInitializeProperly` in the same file

#### Scenario: PreferredLanguageDefaultsToEnUSWithConstructor
- **GIVEN** test method `PreferredLanguageShouldDefaultToEnUSWithConstructor` exists in `src/UnitTests/Core/Model/EmployeeTests.cs`
- **WHEN** a new `Employee("user", "First", "Last", "email@test.com")` is created
- **THEN** `Assert.That(employee.PreferredLanguage, Is.EqualTo("en-US"))` SHALL pass

#### Scenario: PreferredLanguageCanBeSetExplicitly
- **GIVEN** test method `ShouldSetPreferredLanguage` exists in `src/UnitTests/Core/Model/EmployeeTests.cs`
- **WHEN** `employee.PreferredLanguage` is set to `"de-DE"`
- **THEN** `employee.PreferredLanguage.ShouldBe("de-DE")` SHALL pass

#### Scenario: PreferredLanguageIncludedInGetSetTest
- **GIVEN** the existing test `PropertiesShouldGetAndSetProperly` in `src/UnitTests/Core/Model/EmployeeTests.cs`
- **WHEN** the test is updated to include `PreferredLanguage`
- **THEN** the test SHALL set `employee.PreferredLanguage = "fr-FR"` and assert `Assert.That(employee.PreferredLanguage, Is.EqualTo("fr-FR"))`

#### Scenario: PropertiesShouldInitializeProperlyUpdated
- **GIVEN** the existing test `PropertiesShouldInitializeProperly` in `src/UnitTests/Core/Model/EmployeeTests.cs`
- **WHEN** the test is updated to verify `PreferredLanguage` default
- **THEN** the test SHALL include `Assert.That(employee.PreferredLanguage, Is.EqualTo("en-US"))`

### Requirement: Integration test for PreferredLanguage persistence
Integration tests SHALL be added in `src/IntegrationTests/DataAccess/` that verify `PreferredLanguage` survives a database round-trip. Tests SHALL follow the existing patterns in `EmployeeQueryHandlerTests.cs` and `WorkOrderMappingTests.cs`: clean the database first with `new DatabaseTests().Clean()`, use `TestHost.GetRequiredService<DbContext>()` for data setup, and use Shouldly assertions.

#### Scenario: ShouldPersistPreferredLanguage
- **GIVEN** test method `ShouldPersistPreferredLanguage` exists in `src/IntegrationTests/DataAccess/EmployeeQueryHandlerTests.cs`
- **AND** the test calls `new DatabaseTests().Clean()` at the start
- **AND** an `Employee` is created with `PreferredLanguage` set to `"fr-FR"`
- **AND** the employee is saved using `TestHost.GetRequiredService<DbContext>()`
- **WHEN** the employee is retrieved from the database using a fresh `DbContext` scope
- **THEN** `rehydratedEmployee.PreferredLanguage.ShouldBe("fr-FR")` SHALL pass

#### Scenario: ShouldPersistDefaultPreferredLanguage
- **GIVEN** test method `ShouldPersistDefaultPreferredLanguage` exists in `src/IntegrationTests/DataAccess/EmployeeQueryHandlerTests.cs`
- **AND** the test calls `new DatabaseTests().Clean()` at the start
- **AND** an `Employee` is created without setting `PreferredLanguage` (relies on default `"en-US"`)
- **AND** the employee is saved using `TestHost.GetRequiredService<DbContext>()`
- **WHEN** the employee is retrieved from the database using a fresh `DbContext` scope
- **THEN** `rehydratedEmployee.PreferredLanguage.ShouldBe("en-US")` SHALL pass

### Constraints
- The `PreferredLanguage` property SHALL be added to the `Employee` class in `src/Core/Model/Employee.cs` (Core project, no new project references)
- Unit tests SHALL be added to the existing `src/UnitTests/Core/Model/EmployeeTests.cs` file, NOT a new file
- Integration tests SHALL be added to the existing `src/IntegrationTests/DataAccess/EmployeeQueryHandlerTests.cs` file, NOT a new file
- Follow the assertion style already in the target test file: use `Assert.That(x, Is.EqualTo(y))` for consistency with existing tests AND/OR `value.ShouldBe()` for Shouldly-style assertions (both patterns are present in the existing tests)
- Follow AAA pattern without section comments in tests
- Maintain onion architecture (Core has no project references)
- Use TABS for indentation in the SQL migration script
- The migration script SHALL follow the existing pattern: `BEGIN TRANSACTION`, `PRINT`, `ALTER TABLE`, error check, `COMMIT TRANSACTION`
