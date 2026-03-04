## ADDED Requirements

### Requirement: Acceptance test infrastructure for speech translation feature
Acceptance tests for the speech translation feature SHALL be added in a new file `src/AcceptanceTests/WorkOrders/WorkOrderSpeechTests.cs`. The test class SHALL extend `AcceptanceTestBase` and follow the same patterns as `WorkOrderSaveDraftTests.cs` and `WorkOrderAIChatTests.cs`: use `[Test, Retry(2)]`, call `LoginAsCurrentUser()`, use `Page.GetByTestId()` with `nameof(WorkOrderManage.Elements.*)` for element location, and use Shouldly/Playwright assertions.

### Requirement: Acceptance test verifies megaphone buttons are visible on work order screen
An acceptance test SHALL verify that the SpeakTitle and SpeakDescription megaphone buttons are rendered and visible on the work order manage page.

#### Scenario: ShouldRenderSpeakTitleButtonOnWorkOrderManagePage
- **GIVEN** test method `ShouldRenderSpeakTitleButtonOnWorkOrderManagePage` exists in `src/AcceptanceTests/WorkOrders/WorkOrderSpeechTests.cs`
- **AND** the test class extends `AcceptanceTestBase`
- **AND** `[Test, Retry(2)]` attributes are applied
- **WHEN** the test logs in via `LoginAsCurrentUser()`
- **AND** creates a new work order via `CreateAndSaveNewWorkOrder()`
- **AND** navigates to the work order edit page via `ClickWorkOrderNumberFromSearchPage(order)`
- **THEN** `Page.GetByTestId(nameof(WorkOrderManage.Elements.SpeakTitle))` SHALL be visible
- **AND** `await Expect(speakTitleButton).ToBeVisibleAsync()` SHALL pass

#### Scenario: ShouldRenderSpeakDescriptionButtonOnWorkOrderManagePage
- **GIVEN** test method `ShouldRenderSpeakDescriptionButtonOnWorkOrderManagePage` exists in `src/AcceptanceTests/WorkOrders/WorkOrderSpeechTests.cs`
- **AND** the test class extends `AcceptanceTestBase`
- **AND** `[Test, Retry(2)]` attributes are applied
- **WHEN** the test logs in via `LoginAsCurrentUser()`
- **AND** creates a new work order via `CreateAndSaveNewWorkOrder()`
- **AND** navigates to the work order edit page via `ClickWorkOrderNumberFromSearchPage(order)`
- **THEN** `Page.GetByTestId(nameof(WorkOrderManage.Elements.SpeakDescription))` SHALL be visible
- **AND** `await Expect(speakDescriptionButton).ToBeVisibleAsync()` SHALL pass

### Requirement: Acceptance test verifies megaphone button is clickable
An acceptance test SHALL verify that clicking the SpeakTitle megaphone button does not cause an error or page crash. Because browser speech synthesis cannot be verified in headless Playwright, the test SHALL only verify that the button can be clicked without errors.

#### Scenario: ShouldClickSpeakTitleButtonWithoutError
- **GIVEN** test method `ShouldClickSpeakTitleButtonWithoutError` exists in `src/AcceptanceTests/WorkOrders/WorkOrderSpeechTests.cs`
- **AND** `[Test, Retry(2)]` attributes are applied
- **WHEN** the test logs in, creates a work order, navigates to the edit page
- **AND** inputs text into the Title field via `Input(nameof(WorkOrderManage.Elements.Title), "Test speech title")`
- **AND** clicks the SpeakTitle button via `Click(nameof(WorkOrderManage.Elements.SpeakTitle))`
- **THEN** no unhandled exception SHALL occur
- **AND** the page SHALL remain on the work order manage URL (not navigated away or crashed)
- **AND** `await Expect(Page).ToHaveURLAsync(new Regex("/workorder/manage/"))` SHALL pass

#### Scenario: ShouldClickSpeakDescriptionButtonWithoutError
- **GIVEN** test method `ShouldClickSpeakDescriptionButtonWithoutError` exists in `src/AcceptanceTests/WorkOrders/WorkOrderSpeechTests.cs`
- **AND** `[Test, Retry(2)]` attributes are applied
- **WHEN** the test logs in, creates a work order, navigates to the edit page
- **AND** inputs text into the Description field via `Input(nameof(WorkOrderManage.Elements.Description), "Test speech description")`
- **AND** clicks the SpeakDescription button via `Click(nameof(WorkOrderManage.Elements.SpeakDescription))`
- **THEN** no unhandled exception SHALL occur
- **AND** the page SHALL remain on the work order manage URL

### Requirement: Acceptance test verifies megaphone buttons visible on read-only work orders
An acceptance test SHALL verify that the megaphone buttons remain visible even when the work order is in a read-only state for the current user (e.g., a completed work order). The speech feature is useful regardless of whether the user can edit the work order.

#### Scenario: ShouldShowSpeakButtonsOnReadOnlyWorkOrder
- **GIVEN** test method `ShouldShowSpeakButtonsOnReadOnlyWorkOrder` exists in `src/AcceptanceTests/WorkOrders/WorkOrderSpeechTests.cs`
- **AND** `[Test, Retry(2)]` attributes are applied
- **WHEN** the test logs in, creates a work order, assigns it, begins it, completes it
- **AND** navigates back to the completed work order edit page
- **THEN** `Page.GetByTestId(nameof(WorkOrderManage.Elements.SpeakTitle))` SHALL be visible
- **AND** `Page.GetByTestId(nameof(WorkOrderManage.Elements.SpeakDescription))` SHALL be visible
- **AND** `Page.GetByTestId(nameof(WorkOrderManage.Elements.ReadOnlyMessage))` SHALL also be visible (confirming read-only state)

### Constraints
- Test file SHALL be `src/AcceptanceTests/WorkOrders/WorkOrderSpeechTests.cs`
- Test class SHALL extend `AcceptanceTestBase`
- Tests SHALL use `[Test, Retry(2)]` for resilience against intermittent failures
- Tests SHALL use `LoginAsCurrentUser()` to log in before interacting with work orders
- Tests SHALL locate elements via `Page.GetByTestId(nameof(WorkOrderManage.Elements.*))` — the same pattern as all existing acceptance tests
- Tests SHALL use `await Expect(locator).ToBeVisibleAsync()` for visibility assertions
- Tests SHALL use `Click()`, `Input()`, `Select()` helper methods from `AcceptanceTestBase`
- Tests SHALL NOT attempt to verify audio output (Playwright cannot capture browser audio in headless mode) — only verify button visibility and click-without-error
- Tests SHALL NOT require the LLM to be available (no `SkipIfNoChatClient()` in `[SetUp]`) because the translation service degrades gracefully
- Tests SHALL follow the import pattern: `using ClearMeasure.Bootcamp.UI.Shared.Pages;` for `WorkOrderManage.Elements` access
