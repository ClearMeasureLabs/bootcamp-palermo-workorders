## ADDED Requirements

### Requirement: Work order supports optional instructions text

The `WorkOrder` domain model SHALL include an optional `Instructions` property for creator-provided implementation guidance.

#### Scenario: Instructions defaults to empty
- **WHEN** a new `WorkOrder` is created
- **THEN** `Instructions` SHALL be empty by default

#### Scenario: Instructions is truncated at 4000 characters
- **GIVEN** a value longer than 4000 characters
- **WHEN** assigned to `WorkOrder.Instructions`
- **THEN** the stored value SHALL be truncated to 4000 characters

### Requirement: Instructions is persisted in database

The data model SHALL persist `Instructions` for each work order in SQL Server.

#### Scenario: Migration adds nullable instructions column
- **WHEN** database update scripts are applied
- **THEN** `dbo.WorkOrder` SHALL contain `[Instructions] NVARCHAR(4000) NULL`

#### Scenario: Save and rehydrate retains instructions
- **GIVEN** a work order with `Instructions`
- **WHEN** the work order is saved and queried back
- **THEN** the queried work order SHALL contain the same instructions text

### Requirement: Work order manage screen collects instructions

The work order manage UI SHALL render and persist an optional instructions field directly under Description.

#### Scenario: Instructions input is rendered under description
- **WHEN** `/workorder/manage` is rendered
- **THEN** an `InputTextArea` bound to `Model.Instructions` SHALL appear directly below the Description form group

#### Scenario: Instructions value is saved and displayed
- **GIVEN** a user enters instructions and saves the work order
- **WHEN** the work order is opened again in edit mode
- **THEN** the Instructions field SHALL display the previously saved value

### Requirement: MCP work order detail includes instructions

MCP work order tools SHALL support instructions data.

#### Scenario: Create tool accepts optional instructions
- **WHEN** `create-work-order` is called with instructions
- **THEN** the created work order SHALL persist instructions

#### Scenario: Detail tool returns instructions
- **WHEN** `get-work-order` returns a work order
- **THEN** the serialized detail payload SHALL include `Instructions`
