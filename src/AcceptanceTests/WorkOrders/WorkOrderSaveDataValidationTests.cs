using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSaveDataValidationTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task FrontendValidation_WithEmptyTitle_ShouldShowError()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "Valid description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var validationMessage = Page.Locator(".validation-message");
        await Expect(validationMessage).ToContainTextAsync("Title is required");
    }

    [Test, Retry(2)]
    public async Task FrontendValidation_WithEmptyDescription_ShouldShowError()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Valid title");
        await Input(nameof(WorkOrderManage.Elements.Description), "");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var validationMessage = Page.Locator(".validation-message");
        await Expect(validationMessage).ToContainTextAsync("Description is required");
    }

    [Test, Retry(2)]
    public async Task FrontendValidation_WithEmptyTitleAndDescription_ShouldShowBothErrors()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var validationMessages = Page.Locator(".validation-message");
        await Expect(validationMessages.Nth(0)).ToContainTextAsync("Title is required");
        await Expect(validationMessages.Nth(1)).ToContainTextAsync("Description is required");
    }

    [Test, Retry(2)]
    public async Task FrontendValidation_ErrorsClearAfterCorrection_ShouldAllowSubmit()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var validationMessage = Page.Locator(".validation-message");
        await Expect(validationMessage.First).ToBeVisibleAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "Valid title");
        await Input(nameof(WorkOrderManage.Elements.Description), "Valid description");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");

        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 10000 });
        await Expect(Page).ToHaveURLAsync("**/workorder/search");
    }

    [Test, Retry(2)]
    public async Task ServerSideValidation_ShouldDisplayErrorInAlert()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Page.EvaluateAsync(@"
            document.querySelector('[data-testid=""Title""]').removeAttribute('required');
            document.querySelector('[data-testid=""Description""]').removeAttribute('required');
        ");

        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "Valid description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var alertDanger = Page.Locator(".alert-danger");
        await Expect(alertDanger).ToContainTextAsync("Title is required");
    }
}
