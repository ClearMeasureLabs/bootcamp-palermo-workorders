using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderManageValidationTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldShowValidationErrorWhenTitleIsEmpty()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        // Fill in Description only, leave Title empty
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");

        // Try to save
        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        // Verify validation message appears
        var validationSummary = Page.Locator(".validation-summary");
        await Expect(validationSummary).ToBeVisibleAsync();
        await Expect(validationSummary).ToContainTextAsync("The Title field is required");

        await TakeScreenshotAsync(1, "TitleValidationError");
    }

    [Test, Retry(2)]
    public async Task ShouldShowValidationErrorWhenDescriptionIsEmpty()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        // Fill in Title only, leave Description empty
        await Input(nameof(WorkOrderManage.Elements.Title), "Test title");

        // Try to save
        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        // Verify validation message appears
        var validationSummary = Page.Locator(".validation-summary");
        await Expect(validationSummary).ToBeVisibleAsync();
        await Expect(validationSummary).ToContainTextAsync("The Description field is required");

        await TakeScreenshotAsync(1, "DescriptionValidationError");
    }

    [Test, Retry(2)]
    public async Task ShouldShowBothValidationErrorsWhenTitleAndDescriptionAreEmpty()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        // Don't fill in Title or Description

        // Try to save
        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        // Verify validation messages appear
        var validationSummary = Page.Locator(".validation-summary");
        await Expect(validationSummary).ToBeVisibleAsync();
        await Expect(validationSummary).ToContainTextAsync("The Title field is required");
        await Expect(validationSummary).ToContainTextAsync("The Description field is required");

        await TakeScreenshotAsync(1, "BothFieldsValidationError");
    }

    [Test, Retry(2)]
    public async Task ShouldClearValidationErrorsAfterCorrectingFields()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        // Try to save without filling in required fields
        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        // Verify validation messages appear
        var validationSummary = Page.Locator(".validation-summary");
        await Expect(validationSummary).ToBeVisibleAsync();

        await TakeScreenshotAsync(1, "ValidationErrorsShown");

        // Now fill in the required fields
        await Input(nameof(WorkOrderManage.Elements.Title), "Test title");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");

        // Try to save again
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Should navigate to search page (success)
        await Page.WaitForURLAsync("**/workorder/search");
        await TakeScreenshotAsync(2, "SuccessfulSave");
    }

    [Test, Retry(2)]
    public async Task ShouldShowInlineValidationMessageBelowTitleField()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        // Fill in Description only
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");

        // Try to save
        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        // Verify inline validation message appears below Title field
        var validationMessage = Page.Locator(".validation-message").First;
        await Expect(validationMessage).ToBeVisibleAsync();
        await Expect(validationMessage).ToContainTextAsync("The Title field is required");

        await TakeScreenshotAsync(1, "InlineTitleValidation");
    }
}
