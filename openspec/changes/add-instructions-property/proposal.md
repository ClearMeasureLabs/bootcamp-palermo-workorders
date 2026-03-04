## Why

Work orders currently support a Title, Description, and other metadata, but there is no dedicated field for operational instructions. Operators and supervisors need a way to attach step-by-step instructions or procedural notes to a work order, separate from the general description. Adding an `Instructions` property to the `WorkOrder` entity fills this gap and keeps the domain model expressive.

## What Changes

- New `Instructions` string property on the `WorkOrder` domain entity following the same truncation and null-normalization pattern as `Description`
- EF Core mapping for the new column with `HasMaxLength(4000)`
- DbUp migration script adding the `Instructions` column to the `WorkOrder` table
- Blazor UI updated to display and edit the `Instructions` field on work order forms
- Unit tests for property behavior (truncation, null handling)
- Integration tests for persistence round-trip

## Capabilities

### New Capabilities

- `work-order-instructions`: The `WorkOrder` entity gains an `Instructions` property that stores up to 4000 characters of free-text instructions, with null-to-empty-string normalization and automatic truncation

### Modified Capabilities

- `work-order-ui`: The work order create/edit forms display an Instructions text field
- `work-order-persistence`: EF Core mapping and database schema include the new column

## Impact

- **Core**: New property on `WorkOrder` — no new dependencies
- **DataAccess**: Updated `WorkOrderMap` with one additional property mapping
- **Database**: One new DbUp migration script (`028_AddInstructionsToWorkOrder.sql`)
- **UI**: Updated Blazor form components to include the Instructions field
- **Tests**: New unit and integration tests; no changes to existing tests
