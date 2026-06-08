## ADDED Requirements

### Requirement: Database migration adds Instructions column to WorkOrder table
A new DbUp migration script `028_AddInstructionsToWorkOrder.sql` SHALL be created in `src/Database/scripts/Update/` that adds an `Instructions` column to the `dbo.WorkOrder` table.

#### Scenario: Migration adds nullable NVARCHAR(4000) column
- **WHEN** the migration script executes
- **THEN** the `dbo.WorkOrder` table SHALL have a new column `Instructions` of type `NVARCHAR(4000)` with a `NULL` constraint (optional field)

#### Scenario: Migration follows the standard script pattern
- **WHEN** the migration script is examined
- **THEN** it SHALL follow the established pattern:
  1. `BEGIN TRANSACTION` / `GO`
  2. `PRINT N'Adding [Instructions] to [dbo].[WorkOrder]'` / `GO`
  3. `ALTER TABLE [dbo].[WorkOrder] ADD [Instructions] NVARCHAR(4000) NULL` / `GO`
  4. `IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION` / `GO`
  5. `PRINT 'The database update succeeded'`
  6. `COMMIT TRANSACTION` / `GO`

#### Scenario: Migration is idempotent via DbUp journal
- **WHEN** the migration script has already been applied
- **THEN** DbUp's journal table SHALL prevent it from running again

### Constraints
- The script SHALL use TABS for indentation (matching existing migration conventions)
- The column SHALL be `NULL` (not `NOT NULL`) since Instructions is optional and existing rows will have no value
- No default constraint is needed since null is acceptable
- The script number SHALL be `028`, the next sequential number after `027_UpdateWorkOrderDFTDRT.sql`
