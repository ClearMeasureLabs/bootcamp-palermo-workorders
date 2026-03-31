## MODIFIED Requirements

### Requirement: Integration test for Instructions persistence
Integration tests SHALL be added in `src/IntegrationTests/DataAccess/WorkOrderQueryHandlerTests.cs` to verify that the `Instructions` property survives a database round-trip.

#### Scenario: ShouldPersistInstructions
- **GIVEN** test method `ShouldPersistInstructions` exists in `WorkOrderQueryHandlerTests.cs`
- **AND** the test calls `new DatabaseTests().Clean()` at the start
- **AND** a `WorkOrder` is created with `Instructions` set to `"Step 1: Turn off the water. Step 2: Replace the gasket."`
- **AND** the work order is saved via `StateCommandHandler` / `SaveDraftCommand` through `IBus` (following the existing test pattern of seeding via EF Core)
- **WHEN** the work order is retrieved from the database using `WorkOrderByNumberQuery` via `IBus`
- **THEN** `rehydratedWorkOrder.Instructions.ShouldBe("Step 1: Turn off the water. Step 2: Replace the gasket.")` SHALL pass

#### Scenario: ShouldPersistNullInstructionsAsDefault
- **GIVEN** test method `ShouldPersistNullInstructionsAsDefault` exists in `WorkOrderQueryHandlerTests.cs`
- **AND** the test calls `new DatabaseTests().Clean()` at the start
- **AND** a `WorkOrder` is created without setting `Instructions` (relies on domain default of `""`)
- **AND** the work order is saved to the database
- **WHEN** the work order is retrieved from the database
- **THEN** `rehydratedWorkOrder.Instructions` SHALL be `""` or `null` (both are acceptable since EF Core may return `null` from a `NULL` database column)

#### Scenario: SearchShouldReturnHydratedWorkOrderWithInstructions
- **GIVEN** the existing test `SearchShouldReturnHydratedEmployeesWithWorkOrders` in `WorkOrderQueryHandlerTests.cs`
- **WHEN** the test is examined after the Instructions feature is implemented
- **THEN** the seeded work orders with Instructions set SHALL return the Instructions value when retrieved via specification query

### Constraints
- Integration tests SHALL be added to the existing `src/IntegrationTests/DataAccess/WorkOrderQueryHandlerTests.cs` file, NOT a new file
- Tests SHALL follow the existing seeding pattern: clean database with `new DatabaseTests().Clean()`, create entities, attach and save via `DbContext`, then query via `IBus`
- Tests SHALL use Shouldly assertions (e.g., `value.ShouldBe(expected)`) matching the existing test patterns
- Tests SHALL follow AAA pattern without section comments
