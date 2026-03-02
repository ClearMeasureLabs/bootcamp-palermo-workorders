# Lab 08: Database DevOps - Writing a Migration

**Curriculum Section:** Section 05 (Team/Process Design - Database DevOps)
**Estimated Time:** 40 minutes
**Type:** Build

---

## Objective

Add a new column to the database using a DbUp migration script, update the domain model and EF Core mapping, and verify with tests.

---

## Steps

### Step 1: List Existing Migrations

```powershell
ls src/Database/scripts/Update/
```

Note the numbering convention (`###_Description.sql`). Find the highest number; your new migration increments by 1.

### Step 2: Create the Migration Script

Create `src/Database/scripts/Update/024_AddPriorityToWorkOrder.sql`:

```sql
ALTER TABLE dbo.WorkOrder ADD Priority NVARCHAR(20) NULL DEFAULT 'Normal';
```

Use TABS for indentation per project convention.

### Step 3: Update the Domain Model

Add to `src/Core/Model/WorkOrder.cs`:

```csharp
public string? Priority { get; set; } = "Normal";
```

### Step 4: Update the EF Core Mapping

Add to `src/DataAccess/Mappings/WorkOrderMap.cs` inside the `Map` method:

```csharp
entity.Property(p => p.Priority).HasMaxLength(20);
```

### Step 5: Run the Build

```powershell
.\privatebuild.ps1
```

### Step 6: Write Tests

Unit test for default value; integration test that saves Priority "Urgent" and reads it back.

---

## Expected Outcome

- New migration script, updated model, EF mapping, and tests — all green

---

## Discussion Questions

1. Why is the migration a separate SQL script rather than EF Core auto-migration?
2. Why use `NULL DEFAULT 'Normal'` for the new column?
3. If you needed to rename this column after production deployment, what strategy would you use?
