## Context

The `WorkOrder` entity in `src/Core/Model/WorkOrder.cs` already has a `Description` property that normalizes null to empty string and truncates to 4000 characters via a private `getTruncatedString` helper. The new `Instructions` property follows the exact same pattern, reusing the existing helper method. EF Core mapping in `WorkOrderMap.cs` and a DbUp migration complete the data layer. The Blazor UI form components are updated to expose the new field.

## Goals / Non-Goals

**Goals:**
- Add an `Instructions` property to `WorkOrder` with the same behavior as `Description` (null normalization, 4000-char truncation)
- Persist the property to SQL Server via EF Core with a matching column
- Expose the field in the Blazor work order UI
- Cover the new behavior with unit and integration tests

**Non-Goals:**
- Changing the behavior of existing properties
- Adding validation rules beyond truncation (e.g., required field, format checks)
- Modifying the MCP server or API to expose Instructions (can be done in a follow-up)

## Decisions

### Decision 1: Reuse `getTruncatedString` for the Instructions setter

**Rationale:** The `Description` property already uses a private `getTruncatedString(string?)` method that normalizes null to empty string and truncates to 4000 characters. `Instructions` has identical requirements, so reusing the same helper avoids duplication and keeps behavior consistent.

### Decision 2: Database column as `nvarchar(4000)` with NULL allowed

**Rationale:** Matches the `Description` column pattern. EF Core mapping uses `HasMaxLength(4000)` without `IsRequired()`, consistent with how `Description` is mapped. Existing rows will have NULL in the new column, which the entity setter normalizes to empty string on read.

### Decision 3: Single DbUp migration script

**Rationale:** The change is a single `ALTER TABLE ADD COLUMN`. One migration script numbered `028_AddInstructionsToWorkOrder.sql` is sufficient.

## Risks / Trade-offs

- **Minimal risk**: The change is additive — a new nullable column with no constraints. Existing data and behavior are unaffected.
- **UI form length**: Adding another text field increases form size. Acceptable for now; layout refinement can follow.
