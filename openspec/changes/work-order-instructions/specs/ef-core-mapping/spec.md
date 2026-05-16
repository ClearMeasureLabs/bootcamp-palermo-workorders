## CHANGED Requirements

### Requirement: EF Core mapping for Instructions property
The `WorkOrderMap` class in `src/DataAccess/Mappings/WorkOrderMap.cs` SHALL map the `Instructions` property with a maximum length of 4000 characters. The property SHALL NOT be marked as required (it is nullable/optional).

#### Scenario: Instructions is mapped with correct max length
- **WHEN** the EF Core model is built
- **THEN** the `Instructions` property SHALL be configured with `HasMaxLength(4000)`
- **AND** it SHALL NOT have `.IsRequired()` (the column is nullable)

#### Scenario: Instructions mapping is placed after Description
- **WHEN** the `WorkOrderMap.Map()` method is examined
- **THEN** the `entity.Property(e => e.Instructions).HasMaxLength(4000);` line SHALL appear after the `entity.Property(e => e.Description).HasMaxLength(4000);` line and before the `entity.Property(e => e.RoomNumber).HasMaxLength(50);` line

### Constraints
- The mapping SHALL be added to the existing `WorkOrderMap` class in `src/DataAccess/Mappings/WorkOrderMap.cs`
- The mapping SHALL follow the same style as the existing `Description` mapping: `entity.Property(e => e.Instructions).HasMaxLength(4000);`
- The mapping SHALL NOT include `.IsRequired()` since Instructions is optional
