## ADDED Requirements

### Requirement: Translation service translates text via IChatClient
A new service SHALL be created that uses the `ChatClientFactory` from `src/LlmGateway/ChatClientFactory.cs` to obtain an `IChatClient` instance, sends a prompt to translate text from its source language into a target BCP 47 language, and returns the translated text.

#### Scenario: Translate English text to Spanish
- **GIVEN** the `ChatClientFactory.IsChatClientAvailable()` returns `true`
- **WHEN** the translation service is invoked with source text `"Fix the broken pipe in room 201"` and target language `"es-ES"`
- **THEN** the service SHALL send a prompt to the `IChatClient` instructing it to translate the text into Spanish (Spain)
- **AND** the service SHALL return the translated text from the LLM response

#### Scenario: Translate English text to German
- **GIVEN** the `ChatClientFactory.IsChatClientAvailable()` returns `true`
- **WHEN** the translation service is invoked with source text `"Organize Christmas Concert"` and target language `"de-DE"`
- **THEN** the service SHALL return the German translation from the LLM response

#### Scenario: Translate English text to French
- **GIVEN** the `ChatClientFactory.IsChatClientAvailable()` returns `true`
- **WHEN** the translation service is invoked with source text `"Tune and Maintain Church Organ"` and target language `"fr-FR"`
- **THEN** the service SHALL return the French translation from the LLM response

#### Scenario: Target language matches source language (no translation needed)
- **WHEN** the translation service is invoked with source text `"Fix the pipe"` and target language `"en-US"`
- **THEN** the service SHALL return the original text without calling the `IChatClient`

#### Scenario: Chat client is not available
- **GIVEN** the `ChatClientFactory.IsChatClientAvailable()` returns `false` (missing environment variables)
- **WHEN** the translation service is invoked
- **THEN** the service SHALL return the original untranslated text
- **AND** the service SHALL NOT throw an exception

### Requirement: Translation prompt instructs LLM to return only translated text
The system prompt sent to the `IChatClient` SHALL instruct the LLM to translate the provided text into the specified language and return ONLY the translated text with no additional commentary, explanation, or formatting.

#### Scenario: Prompt structure
- **WHEN** a translation is requested for text `"Hello"` into language `"es-ES"`
- **THEN** the system prompt SHALL include instructions such as: "Translate the following text into {language name}. Return ONLY the translated text, nothing else."
- **AND** the user message SHALL contain the text to translate

### Requirement: Translation service is registered in DI
The translation service SHALL be injectable via an interface (e.g., `ITranslationService`) defined in `src/Core/Services/` and implemented in `src/LlmGateway/`. The implementation SHALL receive `ChatClientFactory` via constructor injection.

#### Scenario: Interface is in Core
- **WHEN** the `ITranslationService` interface is examined
- **THEN** it SHALL be in the `ClearMeasure.Bootcamp.Core.Services` namespace
- **AND** it SHALL define a method with signature: `Task<string> TranslateAsync(string text, string targetLanguageCode)`

#### Scenario: Implementation is in LlmGateway
- **WHEN** the `TranslationService` implementation is examined
- **THEN** it SHALL be in the `ClearMeasure.Bootcamp.LlmGateway` namespace
- **AND** it SHALL reference `ChatClientFactory` for obtaining an `IChatClient`

### Requirement: Unit tests for TranslationService
Unit tests SHALL be added in a new file `src/UnitTests/LlmGateway/TranslationServiceTests.cs`. Tests SHALL use the `[TestFixture]` attribute and follow the existing naming convention (e.g., `ShouldDoSomething`). Tests SHALL use Shouldly assertions.

#### Scenario: ShouldReturnOriginalTextWhenTargetLanguageIsEnUS
- **GIVEN** test method `ShouldReturnOriginalTextWhenTargetLanguageIsEnUS` exists in `src/UnitTests/LlmGateway/TranslationServiceTests.cs`
- **WHEN** `TranslateAsync("Fix the pipe", "en-US")` is called
- **THEN** the result SHALL be `"Fix the pipe"` (the original text, unchanged)
- **AND** the `IChatClient` SHALL NOT have been called (verify via a stub/mock that no `GetResponseAsync` invocation occurred)

#### Scenario: ShouldReturnOriginalTextWhenChatClientUnavailable
- **GIVEN** test method `ShouldReturnOriginalTextWhenChatClientUnavailable` exists in `src/UnitTests/LlmGateway/TranslationServiceTests.cs`
- **AND** a stub `ChatClientFactory` is configured so `IsChatClientAvailable()` returns `false`
- **WHEN** `TranslateAsync("Hello", "es-ES")` is called
- **THEN** the result SHALL be `"Hello"` (the original text, unchanged)

#### Scenario: ShouldReturnOriginalTextWhenInputIsEmpty
- **GIVEN** test method `ShouldReturnOriginalTextWhenInputIsEmpty` exists in `src/UnitTests/LlmGateway/TranslationServiceTests.cs`
- **WHEN** `TranslateAsync("", "es-ES")` is called
- **THEN** the result SHALL be `""` (empty string returned without calling LLM)

#### Scenario: ShouldReturnOriginalTextWhenInputIsNull
- **GIVEN** test method `ShouldReturnOriginalTextWhenInputIsNull` exists in `src/UnitTests/LlmGateway/TranslationServiceTests.cs`
- **WHEN** `TranslateAsync(null, "es-ES")` is called
- **THEN** the result SHALL be `null` or `""` (without calling LLM, no exception thrown)

### Requirement: Integration tests for TranslationService
Integration tests SHALL be added in a new file `src/IntegrationTests/LlmGateway/TranslationServiceTests.cs`. These tests SHALL follow the same pattern as `WorkOrderChatHandlerTests.cs`: extend `LlmTestBase` (which skips tests when AI is unavailable via `[SetUp]`) and use `TestHost.GetRequiredService<ChatClientFactory>()` to obtain a real `ChatClientFactory`.

#### Scenario: ShouldTranslateTextToSpanish
- **GIVEN** test method `ShouldTranslateTextToSpanish` exists in `src/IntegrationTests/LlmGateway/TranslationServiceTests.cs`
- **AND** the test class extends `LlmTestBase` (which includes `[SetUp]` that calls `SkipIfNoChatClient()`)
- **AND** a `TranslationService` is constructed with `TestHost.GetRequiredService<ChatClientFactory>()`
- **WHEN** `TranslateAsync("Fix the broken pipe", "es-ES")` is called
- **THEN** the result SHALL be a non-empty string
- **AND** the result SHALL NOT equal `"Fix the broken pipe"` (it was actually translated)

#### Scenario: ShouldTranslateTextToGerman
- **GIVEN** test method `ShouldTranslateTextToGerman` exists in `src/IntegrationTests/LlmGateway/TranslationServiceTests.cs`
- **AND** the test class extends `LlmTestBase`
- **WHEN** `TranslateAsync("Organize Christmas Concert", "de-DE")` is called
- **THEN** the result SHALL be a non-empty string that differs from the input

#### Scenario: ShouldReturnOriginalTextForEnUS
- **GIVEN** test method `ShouldReturnOriginalTextForEnUS` exists in `src/IntegrationTests/LlmGateway/TranslationServiceTests.cs`
- **WHEN** `TranslateAsync("Hello world", "en-US")` is called
- **THEN** the result SHALL be `"Hello world"` (no LLM call made)

### Constraints
- The `ITranslationService` interface SHALL be defined in `src/Core/Services/` (Core project, no new project references)
- The `TranslationService` implementation SHALL be in `src/LlmGateway/` (LlmGateway project, which already references Core)
- Follow the same `IChatClient` usage pattern as `WorkOrderReformatAgent` in `src/UI/Server/WorkOrderReformatAgent.cs`: system prompt + user content, call `GetResponseAsync`, parse the text response
- No new NuGet packages required for translation (uses existing `Microsoft.Extensions.AI` abstractions)
- No tools/function-calling needed for translation — use a simple `GetResponseAsync` call without `ChatOptions.Tools`
- Unit tests SHALL be in a new file `src/UnitTests/LlmGateway/TranslationServiceTests.cs`
- Integration tests SHALL be in a new file `src/IntegrationTests/LlmGateway/TranslationServiceTests.cs` and SHALL extend `LlmTestBase`
- Integration tests that call the LLM SHALL use `LlmTestBase` which auto-skips when the chat client is unavailable
