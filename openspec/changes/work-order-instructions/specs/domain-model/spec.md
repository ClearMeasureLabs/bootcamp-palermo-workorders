## MODIFIED Requirements

### Requirement: WorkOrder has an Instructions property
The `WorkOrder` entity in `src/Core/Model/WorkOrder.cs` SHALL include an `Instructions` property of type `string?` that stores detailed instructions for how to perform the work. The property SHALL use a private backing field that defaults to `""` and SHALL auto-truncate values to a maximum of 4000 characters, following the same pattern as the existing `Description` property.

#### Scenario: Instructions defaults to empty string
- **WHEN** a new `WorkOrder` is created using the parameterless constructor
- **THEN** the `Instructions` property SHALL be `""`

#### Scenario: Instructions can be set to a value
- **WHEN** `Instructions` is set to `"Step 1: Turn off the water supply. Step 2: Remove the old faucet."`
- **THEN** the property SHALL retain that value

#### Scenario: Instructions truncates to 4000 characters
- **WHEN** `Instructions` is set to a string longer than 4000 characters
- **THEN** the property SHALL truncate the value to exactly 4000 characters

#### Scenario: Instructions handles null by normalizing to empty string
- **WHEN** `Instructions` is set to `null`
- **THEN** the property SHALL return `""`

### Constraints
- The `Instructions` property SHALL follow the exact same implementation pattern as the `Description` property: private backing field (`_instructions`), getter returning the field, setter calling `getTruncatedString(value)` to enforce the 4000-character limit
- The property SHALL be placed after `Description` in the class definition for logical grouping
- No new project references SHALL be added to the Core project
