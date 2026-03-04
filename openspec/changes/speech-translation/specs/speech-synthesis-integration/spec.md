## ADDED Requirements

### Requirement: SpeechSynthesis NuGet package is added to UI.Shared
The `Toolbelt.Blazor.SpeechSynthesis` NuGet package (version 11.0.0) SHALL be added to the `src/UI.Shared/UI.Shared.csproj` project to enable browser-based text-to-speech in Blazor components.

#### Scenario: Package is referenced
- **WHEN** the `UI.Shared.csproj` file is examined
- **THEN** it SHALL contain `<PackageReference Include="Toolbelt.Blazor.SpeechSynthesis" Version="11.0.0" />`

### Requirement: SpeechSynthesis service is registered in DI
The `SpeechSynthesis` service SHALL be registered in the application's DI container using `builder.Services.AddSpeechSynthesis()` in the appropriate `Program.cs` startup code (in `src/UI/Server/` or `src/UI/Client/`).

#### Scenario: Service is registered
- **WHEN** the application starts
- **THEN** `SpeechSynthesis` SHALL be resolvable from the DI container
- **AND** Blazor components can inject it via `@inject SpeechSynthesis SpeechSynthesis`

### Requirement: Megaphone button next to Title field
The work order manage page (`src/UI.Shared/Pages/WorkOrderManage.razor`) SHALL display a megaphone/speaker icon button adjacent to the Title input field. The button SHALL be visible regardless of read-only state.

#### Scenario: Megaphone button is rendered next to Title
- **WHEN** the work order manage page is rendered
- **THEN** a button with a speaker/megaphone icon SHALL appear next to the Title field
- **AND** the button SHALL have a `data-testid` attribute of `SpeakTitle` for test automation

#### Scenario: Title megaphone button click triggers translation and speech
- **GIVEN** the work order Title field contains text (e.g., `"Fix the broken pipe"`)
- **AND** the logged-in user's `PreferredLanguage` is `"es-ES"`
- **WHEN** the user clicks the megaphone button next to the Title field
- **THEN** the system SHALL invoke the `ITranslationService` to translate the Title text into `"es-ES"`
- **AND** the system SHALL invoke `SpeechSynthesis.SpeakAsync()` with a `SpeechSynthesisUtterance` containing the translated text
- **AND** the `SpeechSynthesisUtterance.Lang` SHALL be set to the user's `PreferredLanguage` (e.g., `"es-ES"`)

### Requirement: Megaphone button next to Description field
The work order manage page SHALL display a megaphone/speaker icon button adjacent to the Description textarea field, with the same behavior as the Title megaphone button but operating on the Description text.

#### Scenario: Megaphone button is rendered next to Description
- **WHEN** the work order manage page is rendered
- **THEN** a button with a speaker/megaphone icon SHALL appear next to the Description field
- **AND** the button SHALL have a `data-testid` attribute of `SpeakDescription` for test automation

#### Scenario: Description megaphone button click triggers translation and speech
- **GIVEN** the work order Description field contains text (e.g., `"The sanctuary organ needs full maintenance"`)
- **AND** the logged-in user's `PreferredLanguage` is `"de-DE"`
- **WHEN** the user clicks the megaphone button next to the Description field
- **THEN** the system SHALL invoke the `ITranslationService` to translate the Description text into `"de-DE"`
- **AND** the system SHALL invoke `SpeechSynthesis.SpeakAsync()` with a `SpeechSynthesisUtterance` containing the translated text
- **AND** the `SpeechSynthesisUtterance.Lang` SHALL be set to `"de-DE"`

### Requirement: Voice selection matches preferred language
When speaking translated text, the system SHALL attempt to select a `SpeechSynthesisVoice` whose `Lang` property matches the current user's `PreferredLanguage`. If no exact match is found, the system SHALL fall back to setting the `SpeechSynthesisUtterance.Lang` property and let the browser select an appropriate voice.

#### Scenario: Voice matches preferred language
- **GIVEN** the browser has available voices including one with `Lang` = `"es-ES"`
- **AND** the user's `PreferredLanguage` is `"es-ES"`
- **WHEN** the speech synthesis utterance is prepared
- **THEN** the `Voice` property of the `SpeechSynthesisUtterance` SHALL be set to the matching voice
- **AND** the `Lang` property SHALL be set to `"es-ES"`

#### Scenario: No matching voice available
- **GIVEN** the browser does NOT have a voice with `Lang` matching the user's `PreferredLanguage`
- **WHEN** the speech synthesis utterance is prepared
- **THEN** the `Voice` property SHALL be `null`
- **AND** the `Lang` property SHALL still be set to the user's `PreferredLanguage`
- **AND** the browser SHALL use its default voice selection for that language

### Requirement: User's preferred language is available in the component
The `WorkOrderManage` component SHALL retrieve the current user's `PreferredLanguage` from the `Employee` record obtained via `IUserSession.GetCurrentUserAsync()` and make it available for the speech/translation feature.

#### Scenario: Preferred language is loaded on page initialization
- **WHEN** the work order manage page initializes via `OnInitializedAsync`
- **THEN** the current user's `PreferredLanguage` SHALL be stored in a component field for use by the megaphone buttons

### Requirement: Graceful degradation when translation is unavailable
If the `ITranslationService` returns the original text (because the LLM is unavailable or the preferred language is `"en-US"`), the speech synthesis SHALL still speak the original text using the user's preferred language voice.

#### Scenario: LLM unavailable falls back to original text speech
- **GIVEN** the `ChatClientFactory.IsChatClientAvailable()` returns `false`
- **AND** the user's `PreferredLanguage` is `"es-ES"`
- **WHEN** the megaphone button is clicked
- **THEN** the original English text SHALL be spoken using the `"es-ES"` voice/lang setting

#### Scenario: en-US user hears original text
- **GIVEN** the user's `PreferredLanguage` is `"en-US"`
- **WHEN** the megaphone button is clicked
- **THEN** the original text SHALL be spoken in `"en-US"` without translation

### Constraints
- The `Toolbelt.Blazor.SpeechSynthesis` package version 11.0.0 SHALL be added to `src/UI.Shared/UI.Shared.csproj` only
- The megaphone buttons SHALL use a Unicode speaker character (e.g., `\U0001F4E2` or `\U0001F50A`) or an HTML entity as the icon — no additional icon library packages
- The `ITranslationService` SHALL be injected into the `WorkOrderManage` component via `[Inject]`
- The `SpeechSynthesis` service SHALL be injected into the `WorkOrderManage` component via `@inject` or `[Inject]`
- The megaphone button click handler SHALL be an `async Task` method to properly `await` translation and speech
- The component SHALL NOT block the UI thread during translation or speech
- Follow the existing code-behind pattern in `WorkOrderManage.razor.cs` for new methods
