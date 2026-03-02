# Lab 04: Database DevOps - Writing a Migration

**Curriculum Section:** Section 05 (Team/Process Design - Database DevOps)
**Estimated Time:** 40 minutes
**Type:** Build

---

## Objective

Add a new `Instructions` column to the `WorkOrder` table using a DbUp migration script, update the domain model and EF Core mapping, and verify with the private build.

---

## Steps

### Step 1: List Existing Migrations

```powershell
ls src/Database/scripts/Update/
```

Note the numbering convention (`###_Description.sql`). Find the highest number; your new migration increments by 1.

### Step 2: Create the Migration Script

Create `src/Database/scripts/Update/024_AddInstructionsToWorkOrder.sql`:

```sql
ALTER TABLE dbo.WorkOrder ADD Instructions NVARCHAR(4000) NULL
```

Use TABS for indentation per project convention.

### Step 3: Update the Domain Model

Add to `src/Core/Model/WorkOrder.cs`:

```csharp
public string? Instructions { get; set; }
```

### Step 4: Update the EF Core Mapping

Add to `src/DataAccess/Mappings/WorkOrderMap.cs` inside the `Map` method, after the existing `RoomNumber` mapping:

```csharp
entity.Property(e => e.Instructions).HasMaxLength(4000);
```

### Step 5: Run the Build

```powershell
.\PrivateBuild.ps1
```

The private build will run the migration against the database, compile with the updated model, and execute all existing tests. All tests should pass.

### Step 6: Verify the Migration

After a successful build, confirm the column exists by checking that the integration tests pass — the `WorkOrderMappingTests` round-trip test proves EF Core can read and write all mapped columns.

---

## Expected Outcome

- New migration script `024_AddInstructionsToWorkOrder.sql` in place
- `WorkOrder` domain model has an `Instructions` property
- EF Core mapping updated with `HasMaxLength(4000)`
- Private build passes with all tests green
