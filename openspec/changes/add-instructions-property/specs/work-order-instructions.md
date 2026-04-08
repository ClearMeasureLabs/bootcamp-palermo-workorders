## Capability: work-order-instructions

### Overview

The `WorkOrder` entity gains an `Instructions` property for storing procedural instructions associated with a work order. The property follows the same behavioral contract as `Description`.

### Behavior

| Input | Stored Value |
|-------|-------------|
| `null` | `""` (empty string) |
| `""` | `""` |
| String ≤ 4000 chars | Stored as-is |
| String > 4000 chars | Truncated to first 4000 characters |

### Database Schema

```sql
ALTER TABLE [dbo].[WorkOrder]
    ADD [Instructions] NVARCHAR(4000) NULL;
```

### EF Core Mapping

```csharp
entity.Property(e => e.Instructions).HasMaxLength(4000);
```

### Affected Files

- `src/Core/Model/WorkOrder.cs` — New property
- `src/DataAccess/Mappings/WorkOrderMap.cs` — New mapping line
- `src/Database/scripts/Update/028_AddInstructionsToWorkOrder.sql` — Migration
- `src/UnitTests/` — New test class or methods
- `src/IntegrationTests/` — New persistence test
