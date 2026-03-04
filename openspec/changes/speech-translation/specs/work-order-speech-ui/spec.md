## ADDED Requirements

### Requirement: Megaphone button renders next to Title on Work Order screen
The `WorkOrderManage.razor` component SHALL display a clickable megaphone/speaker button immediately adjacent to the Title `InputText` field. The button SHALL be inside the same `<div class="form-group">` as the Title field.

#### Scenario: Button placement in markup
- **WHEN** the work order manage page markup is examined
- **THEN** the Title form group SHALL contain the `InputText` for Title followed by a `<button>` element with `data-testid="SpeakTitle"`
- **AND** the button SHALL display a speaker icon (Unicode character)
- **AND** the button SHALL have `type="button"` to prevent form submission

### Requirement: Megaphone button renders next to Description on Work Order screen
The `WorkOrderManage.razor` component SHALL display a clickable megaphone/speaker button immediately adjacent to the Description `InputTextArea` field.

#### Scenario: Button placement in markup
- **WHEN** the work order manage page markup is examined
- **THEN** the Description form group SHALL contain the `InputTextArea` for Description followed by a `<button>` element with `data-testid="SpeakDescription"`
- **AND** the button SHALL display a speaker icon (Unicode character)
- **AND** the button SHALL have `type="button"` to prevent form submission

### Requirement: Clicking megaphone translates and speaks field text
The click handler for each megaphone button SHALL:
1. Read the current text value from the corresponding field (`Model.Title` or `Model.Description`)
2. Retrieve the logged-in user's `PreferredLanguage`
3. Call `ITranslationService.TranslateAsync(text, preferredLanguage)` to translate the text
4. Call `SpeechSynthesis.GetVoicesAsync()` to get available browser voices
5. Select a voice whose `Lang` starts with the user's preferred language code (e.g., `"es"` prefix for `"es-ES"`)
6. Create a `SpeechSynthesisUtterance` with the translated text, the matched voice, and `Lang` set to the preferred language
7. Call `SpeechSynthesis.SpeakAsync(utterance)` to speak the text aloud

#### Scenario: Full flow for Title megaphone with Spanish user
- **GIVEN** `Model.Title` is `"Fix the broken pipe in room 201"`
- **AND** the current user's `PreferredLanguage` is `"es-ES"`
- **AND** the browser has a voice with `Lang` starting with `"es"`
- **WHEN** the SpeakTitle button is clicked
- **THEN** `ITranslationService.TranslateAsync("Fix the broken pipe in room 201", "es-ES")` is called
- **AND** the translated text is spoken using the Spanish voice with `Lang = "es-ES"`

#### Scenario: Full flow for Description megaphone with German user
- **GIVEN** `Model.Description` is `"The sanctuary organ needs full maintenance and tuning"`
- **AND** the current user's `PreferredLanguage` is `"de-DE"`
- **WHEN** the SpeakDescription button is clicked
- **THEN** `ITranslationService.TranslateAsync("The sanctuary organ needs full maintenance and tuning", "de-DE")` is called
- **AND** the translated text is spoken using a German voice with `Lang = "de-DE"`

#### Scenario: Empty field text does nothing
- **GIVEN** `Model.Title` is `null` or empty
- **WHEN** the SpeakTitle button is clicked
- **THEN** no translation or speech SHALL occur

### Requirement: Component injects required services
The `WorkOrderManage` component (`.razor.cs` code-behind) SHALL inject the following services:

#### Scenario: Required injections
- **WHEN** the `WorkOrderManage` component class is examined
- **THEN** it SHALL have `[Inject] public ITranslationService? TranslationService { get; set; }` 
- **AND** it SHALL have `[Inject] public SpeechSynthesis? SpeechSynthesis { get; set; }`

### Requirement: Current user preferred language is stored
The `WorkOrderManage` component SHALL store the current user's `PreferredLanguage` in a private field during `LoadWorkOrder()`.

#### Scenario: PreferredLanguage captured during load
- **WHEN** `LoadWorkOrder()` retrieves the current user via `UserSession.GetCurrentUserAsync()`
- **THEN** the user's `PreferredLanguage` SHALL be stored in a private field (e.g., `_preferredLanguage`)

### Requirement: Elements enum updated for test automation
The `Elements` enum in `WorkOrderManage.razor` SHALL include entries for the new megaphone buttons.

#### Scenario: Elements enum has speech button entries
- **WHEN** the `Elements` enum is examined
- **THEN** it SHALL include `SpeakTitle` and `SpeakDescription` entries

### Requirement: bUnit tests for megaphone button rendering
bUnit tests SHALL be added in a new file `src/UnitTests/UI.Shared/Pages/WorkOrderManageSpeechTests.cs`. Tests SHALL follow the existing bUnit pattern in `WorkOrderManageAttachmentsTests.cs`: use `Bunit.TestContext`, register stub services via DI (`StubBus` or a local stub `IBus`, `StubUiBus`, stub `IUserSession`, stub `IWorkOrderBuilder`, stub `ITranslationService`, and stub `SpeechSynthesis`), render `WorkOrderManage`, and locate elements by `data-testid`.

#### Scenario: ShouldRenderSpeakTitleButton
- **GIVEN** test method `ShouldRenderSpeakTitleButton` exists in `src/UnitTests/UI.Shared/Pages/WorkOrderManageSpeechTests.cs`
- **AND** the test creates a `Bunit.TestContext`, registers all required stubs (IBus, IUiBus, IWorkOrderBuilder, IUserSession, ITranslationService, SpeechSynthesis), and renders `WorkOrderManage`
- **WHEN** `component.Find($"[data-testid='{WorkOrderManage.Elements.SpeakTitle}']")` is called
- **THEN** the element SHALL be found (not null)
- **AND** the element SHALL be a `<button>` with `type="button"`

#### Scenario: ShouldRenderSpeakDescriptionButton
- **GIVEN** test method `ShouldRenderSpeakDescriptionButton` exists in `src/UnitTests/UI.Shared/Pages/WorkOrderManageSpeechTests.cs`
- **AND** the test creates a `Bunit.TestContext`, registers all required stubs, and renders `WorkOrderManage`
- **WHEN** `component.Find($"[data-testid='{WorkOrderManage.Elements.SpeakDescription}']")` is called
- **THEN** the element SHALL be found (not null)
- **AND** the element SHALL be a `<button>` with `type="button"`

#### Scenario: SpeakTitleButtonShouldInvokeTranslationService
- **GIVEN** test method `SpeakTitleButtonShouldInvokeTranslationService` exists in `src/UnitTests/UI.Shared/Pages/WorkOrderManageSpeechTests.cs`
- **AND** the stub `IUserSession` returns an `Employee` with `PreferredLanguage = "es-ES"`
- **AND** the stub `ITranslationService` records calls and returns translated text
- **AND** the rendered work order has `Title = "Test title"`
- **WHEN** the `SpeakTitle` button's `@onclick` is triggered via `component.Find(...).ClickAsync()`
- **THEN** the stub `ITranslationService.TranslateAsync("Test title", "es-ES")` SHALL have been called

### Constraints
- The megaphone buttons SHALL NOT be inside the `<EditForm>` submit flow — they use `type="button"`
- The `@onclick` handler SHALL call an `async Task` method (e.g., `SpeakTitleAsync()`, `SpeakDescriptionAsync()`)
- The component SHALL use `@using Toolbelt.Blazor.SpeechSynthesis` in the `.razor` file or via `_Imports.razor`
- Voice matching SHALL use `StartsWith` on the language code prefix (e.g., `voice.Lang.StartsWith("es")` for `"es-ES"`) to handle regional variants
- The megaphone buttons SHALL be visually styled as small inline buttons that do not disrupt the existing form layout
- bUnit tests SHALL be in a new file `src/UnitTests/UI.Shared/Pages/WorkOrderManageSpeechTests.cs`
- bUnit tests SHALL follow the same stub patterns as `WorkOrderManageAttachmentsTests.cs` (private stub classes implementing interfaces, registered via `ctx.Services.AddSingleton<T>()`)
- bUnit tests SHALL use `using TestContext = Bunit.TestContext;` to avoid conflicts with NUnit's `TestContext`
- bUnit tests SHALL use Shouldly assertions (e.g., `element.ShouldNotBeNull()`)
