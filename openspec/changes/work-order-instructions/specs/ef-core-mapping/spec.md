## MODIFIED Requirements

### Requirement: WorkOrderMap maps the Instructions property
The `WorkOrderMap` class in `src/DataAccess/Mappings/WorkOrderMap.cs` SHALL map the `Instructions` property to the `Instructions` column in the `dbo.WorkOrder` table.

#### Scenario: Instructions is mapped with correct constraints
- **WHEN** the EF Core model is built
- **THEN** the `Instructions` property SHALL be configured with `HasMaxLength(4000)`
- **AND** the property SHALL NOT be marked as `IsRequired()` (it is optional/nullable)

#### Scenario: Instructions mapping placement
- **WHEN** the `WorkOrderMap.Map()` method is examined
- **THEN** the `Instructions` mapping SHALL be placed after the `Description` mapping, before `RoomNumber`, for logical grouping:
  ```csharp
  entity.Property(e => e.Description).HasMaxLength(4000);
  entity.Property(e => e.Instructions).HasMaxLength(4000);
  entity.Property(e => e.RoomNumber).HasMaxLength(50);
  ```

### Constraints
- Follow the existing mapping pattern: `entity.Property(e => e.PropertyName).HasMaxLength(N)`
- Do NOT mark `Instructions` as `IsRequired()` since it is an optional field
- The mapping SHALL be in the same `modelBuilder.Entity<WorkOrder>()` configuration block as the other properties
