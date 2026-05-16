## CHANGED Requirements

### Requirement: WorkOrderManageModel has an Instructions property
The `WorkOrderManageModel` class in `src/UI.Shared/Models/WorkOrderManageModel.cs` SHALL include an `Instructions` property of type `string?`. The property SHALL NOT have a `[Required]` attribute because Instructions is optional.

#### Scenario: Instructions property exists on model
- **WHEN** the `WorkOrderManageModel` class is examined
- **THEN** it SHALL have a `public string? Instructions { get; set; }` property
- **AND** the property SHALL NOT have `[Required]` attribute
- **AND** the property SHALL appear after `Description` and before `IsReadOnly` for logical grouping

### Requirement: Instructions InputTextArea renders on WorkOrderManage page
The `WorkOrderManage.razor` component SHALL display an `InputTextArea` for the Instructions field, placed immediately after the Description field and before the Room field.

#### Scenario: Instructions field placement in markup
- **WHEN** the work order manage page markup is examined
- **THEN** a new `<div class="form-group">` SHALL appear after the Description form group and before the Room form group
- **AND** it SHALL contain a `<label for="Instructions" class="form-label">Instructions:</label>`
- **AND** it SHALL contain an `<InputTextArea data-testid="@Elements.Instructions" id="Instructions" @bind-Value="Model.Instructions" class="form-control input-textarea" disabled="@Model.IsReadOnly"/>`

#### Scenario: Instructions field respects read-only mode
- **GIVEN** the work order is in a read-only state for the current user
- **WHEN** the work order manage page is rendered
- **THEN** the Instructions `InputTextArea` SHALL have `disabled="true"`

### Requirement: Elements enum updated for Instructions
The `Elements` enum in `WorkOrderManage.razor` SHALL include an entry for the Instructions field for test automation.

#### Scenario: Elements enum has Instructions entry
- **WHEN** the `Elements` enum is examined
- **THEN** it SHALL include an `Instructions` entry
- **AND** the entry SHALL appear after `Description` in the enum

### Requirement: CreateViewModel maps Instructions from WorkOrder to model
The `CreateViewModel()` method in `WorkOrderManage.razor.cs` SHALL map the `Instructions` property from the `WorkOrder` entity to the `WorkOrderManageModel`.

#### Scenario: Instructions mapped in CreateViewModel
- **WHEN** the `CreateViewModel()` method is examined
- **THEN** the `WorkOrderManageModel` initialization SHALL include `Instructions = workOrder.Instructions`
- **AND** this line SHALL appear after `Description = workOrder.Description,` and before `RoomNumber = workOrder.RoomNumber,`

### Requirement: HandleSubmit maps Instructions from model back to WorkOrder
The `HandleSubmit()` method in `WorkOrderManage.razor.cs` SHALL map the `Instructions` property from the model back to the `WorkOrder` entity before sending the state command.

#### Scenario: Instructions mapped in HandleSubmit
- **WHEN** the `HandleSubmit()` method is examined
- **THEN** `workOrder.Instructions = Model.Instructions;` SHALL appear after `workOrder.Description = Model.Description;` and before `workOrder.RoomNumber = Model.RoomNumber;`

### Constraints
- The Instructions field SHALL be rendered as an `<InputTextArea>` (not `<InputText>`) since it holds multi-line detailed instructions
- The field SHALL use `data-testid="@Elements.Instructions"` for test automation
- The field SHALL be placed below Description and above Room in the form, matching the issue requirement ("put it underneath description")
- The field SHALL NOT have a "Speak" button (speech synthesis is scoped to Title and Description only)
- The field SHALL respect the existing `disabled="@Model.IsReadOnly"` pattern
- Changes SHALL be made to existing files only: `WorkOrderManage.razor`, `WorkOrderManage.razor.cs`, and `WorkOrderManageModel.cs`
