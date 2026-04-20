## ADDED Requirements

### Requirement: Database migration adds Instructions column to WorkOrder table
A new DbUp migration script `028_AddInstructionsToWorkOrder.sql` SHALL be created in `src/Database/scripts/Update/` that adds an `Instructions` column to the `dbo.WorkOrder` table.

#### Scenario: Migration adds nullable NVARCHAR(4000) column
- **WHEN** the migration script executes
- **THEN** the `dbo.WorkOrder` table SHALL have a new column `Instructions` of type `NVARCHAR(4000)` with a `NULL` constraint (the column is optional)

#### Scenario: Migration follows existing script pattern
- **WHEN** the migration script is examined
- **THEN** it SHALL follow the established pattern: `BEGIN TRANSACTION`, `GO`, `PRINT`, `GO`, `ALTER TABLE`, `GO`, error check with `IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION`, `GO`, `PRINT 'The database update succeeded'`, `COMMIT TRANSACTION`, `GO`

#### Scenario: Migration script uses correct numbering
- **GIVEN** the highest existing migration script is `027_UpdateWorkOrderDFTDRT.sql`
- **WHEN** the new migration script is created
- **THEN** it SHALL be named `028_AddInstructionsToWorkOrder.sql`

### Constraints
- The migration script SHALL be placed in `src/Database/scripts/Update/`
- The script SHALL use TABS for indentation (per project convention in CLAUDE.md)
- The script SHALL follow the exact pattern established by `024_ExtendWorkOrderTitleLength.sql` and other existing migration scripts
- The `Instructions` column SHALL be `NULL` (not `NOT NULL`) because it is an optional field
- The column type SHALL be `NVARCHAR(4000)` to match the 4000-character maximum from the issue requirement
