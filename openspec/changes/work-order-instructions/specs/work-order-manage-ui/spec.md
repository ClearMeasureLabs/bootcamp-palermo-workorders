## MODIFIED Requirements

### Requirement: Instructions field renders on the Work Order manage screen
The `WorkOrderManage.razor` component SHALL display an Instructions `InputTextArea` field below the Description field, following the same form-group pattern.

#### Scenario: Instructions textarea placement in markup
- **WHEN** the work order manage page markup is examined
- **THEN** the form grid SHALL contain a form-group for Instructions placed after the Description form-group and before the RoomNumber form-group
- **AND** the form-group SHALL contain a `<label>` with text `"Instructions:"` and `for="Instructions"`
- **AND** the form-group SHALL contain an `<InputTextArea>` with `data-testid="@Elements.Instructions"`, `id="Instructions"`, `@bind-Value="Model.Instructions"`, `class="form-control input-textarea"`, and `disabled="@Model.IsReadOnly"`

#### Scenario: Megaphone button renders next to Instructions
- **WHEN** the Instructions form-group markup is examined
- **THEN** the `<InputTextArea>` SHALL be followed by a `<button>` element with `data-testid="@Elements.SpeakInstructions"`
- **AND** the button SHALL have `type="button"` to prevent form submission
- **AND** the button SHALL display a speaker icon (Unicode character `&#x1F50A;`)
- **AND** the button SHALL have `aria-label="Speak instructions"` and `title="Speak instructions"`
- **AND** the button SHALL have `@onclick="SpeakInstructionsAsync"`

### Requirement: Elements enum includes Instructions entries
The `Elements` enum in `WorkOrderManage.razor` SHALL include entries for the Instructions field and its speak button.

#### Scenario: Elements enum has Instructions entries
- **WHEN** the `Elements` enum is examined
- **THEN** it SHALL include `Instructions` and `SpeakInstructions` entries

### Requirement: SpeakInstructionsAsync method exists in code-behind
The `WorkOrderManage.razor.cs` code-behind SHALL include a `SpeakInstructionsAsync()` method that speaks the Instructions text.

#### Scenario: SpeakInstructionsAsync calls SpeakTextAsync
- **WHEN** `SpeakInstructionsAsync()` is invoked
- **THEN** it SHALL call `SpeakTextAsync(Model.Instructions)`, following the same pattern as `SpeakTitleAsync()` and `SpeakDescriptionAsync()`

### Requirement: Instructions is mapped in CreateViewModel
The `CreateViewModel()` method in `WorkOrderManage.razor.cs` SHALL map `Instructions` from the domain entity to the view model.

#### Scenario: CreateViewModel maps Instructions
- **WHEN** `CreateViewModel()` constructs a `WorkOrderManageModel` from a `WorkOrder`
- **THEN** the `WorkOrderManageModel.Instructions` SHALL be set to `workOrder.Instructions`

### Requirement: Instructions is mapped in HandleSubmit
The `HandleSubmit()` method in `WorkOrderManage.razor.cs` SHALL map `Instructions` from the view model back to the domain entity before executing the state command.

#### Scenario: HandleSubmit maps Instructions back to domain
- **WHEN** `HandleSubmit()` prepares the work order for the state command
- **THEN** `workOrder.Instructions` SHALL be set to `Model.Instructions`
- **AND** this assignment SHALL appear alongside the existing `workOrder.Title`, `workOrder.Description`, and `workOrder.RoomNumber` assignments

### Constraints
- The Instructions form-group SHALL follow the exact same HTML/Blazor structure as the Description form-group
- The `InputTextArea` SHALL use `input-textarea` CSS class (same as Description)
- The field SHALL be disabled when `Model.IsReadOnly` is `true`
- The speak button SHALL follow the exact same pattern as `SpeakTitle` and `SpeakDescription` buttons
