## ADDED Requirements

### Requirement: WorkOrder has an Instructions property
The `WorkOrder` entity in `src/Core/Model/WorkOrder.cs` SHALL include an `Instructions` property of type `string?` that stores detailed instructions for how to perform the work order. The property SHALL be optional (nullable), default to `null`, and support up to 4000 characters.

#### Scenario: Instructions defaults to null
- **WHEN** a new `WorkOrder` is created using the default constructor
- **THEN** the `Instructions` property SHALL be `null`

#### Scenario: Instructions can be set and retrieved
- **WHEN** `workOrder.Instructions` is set to `"Step 1: Turn off the water main. Step 2: Replace the gasket."`
- **THEN** `workOrder.Instructions` SHALL return `"Step 1: Turn off the water main. Step 2: Replace the gasket."`

#### Scenario: Instructions truncates to 4000 characters
- **GIVEN** a string of 4001 characters
- **WHEN** `workOrder.Instructions` is set to that string
- **THEN** `workOrder.Instructions.Length` SHALL be `4000`
- **AND** the value SHALL be the first 4000 characters of the original string

#### Scenario: Instructions handles null input
- **WHEN** `workOrder.Instructions` is set to `null`
- **THEN** `workOrder.Instructions` SHALL be `string.Empty`

### Requirement: Instructions uses the same truncation logic as Description
The `Instructions` property SHALL use the same `getTruncatedString()` private method that `Description` already uses for its setter. The backing field SHALL be `private string? _instructions = null;` and the setter SHALL call `_instructions = getTruncatedString(value)` only when the value is not null, otherwise it SHALL remain null.

**Implementation note:** Because `getTruncatedString()` converts `null` to `string.Empty`, and the issue states Instructions is optional, the Instructions property setter needs slightly different behavior than Description: when the value is `null`, it should stay `null` (not be converted to empty string). When the value is non-null, it should be truncated the same way as Description.

#### Scenario: Instructions is null by default unlike Description
- **WHEN** a new `WorkOrder` is created
- **THEN** `Instructions` SHALL be `null`
- **AND** `Description` SHALL be `""` (empty string)

### Constraints
- The `Instructions` property SHALL be added to `src/Core/Model/WorkOrder.cs` (Core project, no new project references)
- Maintain onion architecture (Core has no project references)
- The property SHALL appear after `Description` and before `RoomNumber` in the class for logical grouping
- The `Instructions` property SHALL NOT have a `[Required]` attribute — it is optional
- The max length of 4000 characters matches the issue requirement ("make it in our car 4000" = NVARCHAR(4000))
