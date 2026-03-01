# Lab 08: Database DevOps - Writing a Migration

**Curriculum Section:** Section 05 (Team/Process Design - Database DevOps)
**Estimated Time:** 40 minutes
**Type:** Build

---

## Objective

Add a new column to the database using a DbUp migration script, update the domain model and EF Core mapping, and verify the full stack with tests. Understand why database changes are deployed independently from application code.

---

## Context

The curriculum asks: *"When it comes to deployments, the database is fundamentally different. Why is that?"*

Database changes are:
- **Irreversible** — you cannot "undeploy" a column once data exists in it
- **Shared state** — multiple application versions may run against the same database
- **Ordered** — migrations must run in sequence

This project uses **DbUp** with numbered SQL scripts in `src/Database/scripts/Update/`.

---

## Steps

### Step 1: List Existing Migrations

```powershell
ls src/Database/scripts/Update/
```

Note the naming convention: `###_Description.sql`. Find the highest number (currently `023`). Your new migration will be `024`.

### Step 2: Create the Migration Script

Create `src/Database/scripts/Update/024_AddPriorityToWorkOrder.sql`:

```sql
ALTER TABLE dbo.WorkOrder ADD Priority NVARCHAR(20) NULL DEFAULT 'Normal';
```

**Important conventions:**
- Use TABS for indentation (per project convention)
- Use `NULL DEFAULT` for new columns to avoid breaking existing rows
- Keep migrations small and focused

### Step 3: Update the Domain Model

Open `src/Core/Model/WorkOrder.cs`. Add the new property:

```csharp
public string? Priority { get; set; } = "Normal";
```

Add it after the existing properties (e.g., after `CompletedDate`).

### Step 4: Update the EF Core Mapping

Open `src/DataAccess/Mappings/WorkOrderMap.cs`. Add the property mapping inside the `Map` method:

```csharp
entity.Property(p => p.Priority).HasMaxLength(20);
```

### Step 5: Run the Build (Applies Migration)

```powershell
.\privatebuild.ps1
```

The build runs `Setup-DatabaseForBuild`, which executes DbUp and applies your migration. Verify the build passes.

### Step 6: Write a Unit Test

Add to `src/UnitTests/Core/Model/WorkOrderTests.cs`:

```csharp
[Test]
public void Priority_WhenNotSet_ShouldDefaultToNormal()
{
    var order = new WorkOrder();

    order.Priority.ShouldBe("Normal");
}
```

### Step 7: Write an Integration Test

Add to a test file in `src/IntegrationTests/DataAccess/`:

```csharp
[Test]
public async Task ShouldPersistWorkOrderPriority()
{
    new DatabaseTests().Clean();

    var creator = Faker<Employee>();
    var context = TestHost.GetRequiredService<DbContext>();
    context.Add(creator);

    var workOrder = Faker<WorkOrder>();
    workOrder.Creator = creator;
    workOrder.Priority = "Urgent";
    context.Add(workOrder);
    await context.SaveChangesAsync();

    var readContext = TestHost.GetRequiredService<DbContext>();
    var persisted = readContext.Find<WorkOrder>(workOrder.Id);
    persisted.ShouldNotBeNull();
    persisted!.Priority.ShouldBe("Urgent");
}
```

### Step 8: Verify Everything

```powershell
dotnet test src/UnitTests --configuration Release
dotnet test src/IntegrationTests --configuration Release
```

All tests should pass.

---

## Expected Outcome

- A new database migration script (`024_AddPriorityToWorkOrder.sql`)
- Updated domain model with `Priority` property
- Updated EF Core mapping
- A unit test for the default value
- An integration test for persistence
- Green build

---

## Discussion Questions

1. Why is the database migration a **separate SQL script** rather than EF Core's automatic migration? (Explicit control, reviewable, auditable, runs independently of application deployment)
2. The migration uses `NULL DEFAULT 'Normal'`. Why is this important for existing rows? What would happen with `NOT NULL` and no default?
3. Why are database migrations numbered sequentially? What would happen if two developers both created migration `024`?
4. The curriculum mentions "Database Deployment Pipeline" as separate from application deployment. Why is this distinction critical?
5. If you needed to rename the `Priority` column to `UrgencyLevel` after production deployment, what migration strategy would you use? (Add new column, copy data, drop old column — never rename in place)
