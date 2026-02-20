using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSaveDataValidationTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task FrontendValidation_WithEmptyTitle_ShouldShowErrorMessage()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Description), "Valid description");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");

        var validationMessage = Page.Locator(".validation-message").First;
        await Expect(validationMessage).ToBeVisibleAsync();
        await Expect(validationMessage).ToContainTextAsync("Title is required");
    }

    [Test, Retry(2)]
    public async Task FrontendValidation_WithEmptyDescription_ShouldShowErrorMessage()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Valid title");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");

        var validationMessage = Page.Locator(".validation-message").Nth(1);
        await Expect(validationMessage).ToBeVisibleAsync();
        await Expect(validationMessage).ToContainTextAsync("Description is required");
    }

    [Test, Retry(2)]
    public async Task FrontendValidation_WithEmptyTitleAndDescription_ShouldShowBothErrorMessages()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");

        var validationMessages = Page.Locator(".validation-message");
        await Expect(validationMessages).ToHaveCountAsync(2);
        await Expect(validationMessages.First).ToContainTextAsync("Title is required");
        await Expect(validationMessages.Nth(1)).ToContainTextAsync("Description is required");
    }

    [Test, Retry(2)]
    public async Task FrontendValidation_AfterCorrectingErrors_ShouldClearErrorMessages()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");

        var validationMessages = Page.Locator(".validation-message");
        await Expect(validationMessages).ToHaveCountAsync(2);

        await Input(nameof(WorkOrderManage.Elements.Title), "Valid title");
        await Input(nameof(WorkOrderManage.Elements.Description), "Valid description");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");

        await Page.WaitForURLAsync("**/workorder/search");
    }

    [Test, Retry(2)]
    public async Task ServerSideValidation_WithError_ShouldDisplayErrorPopup()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Page.EvaluateAsync(@"
            const originalFetch = window.fetch;
            window.fetch = function(url, options) {
                if (url.includes('api/blazor-wasm-single-api')) {
                    return Promise.resolve(new Response(
                        'Title is required; Description is required',
                        { status: 400, statusText: 'Bad Request' }
                    ));
                }
                return originalFetch(url, options);
            };
        ");

        await Input(nameof(WorkOrderManage.Elements.Title), "Test");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");

        var errorAlert = Page.Locator(".alert-danger");
        await Expect(errorAlert).ToBeVisibleAsync();
        await Expect(errorAlert).ToContainTextAsync("Title is required");
    }
}
