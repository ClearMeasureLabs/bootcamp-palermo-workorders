## MODIFIED Requirements

### Requirement: WorkOrderManageModel has an Instructions property
The `WorkOrderManageModel` class in `src/UI.Shared/Models/WorkOrderManageModel.cs` SHALL include an `Instructions` property of type `string?` for binding the Instructions form field on the work order manage page.

#### Scenario: Instructions property exists on the view model
- **WHEN** the `WorkOrderManageModel` class is examined
- **THEN** it SHALL have a `public string? Instructions { get; set; }` property

#### Scenario: Instructions property placement
- **WHEN** the property declarations are examined
- **THEN** `Instructions` SHALL be placed after the `Description` property for logical grouping

### Constraints
- The `Instructions` property SHALL NOT have a `[Required]` attribute — it is an optional field (unlike `Title` and `Description` which are `[Required]`)
- The property type SHALL be `string?` (nullable) matching the pattern of `RoomNumber`
