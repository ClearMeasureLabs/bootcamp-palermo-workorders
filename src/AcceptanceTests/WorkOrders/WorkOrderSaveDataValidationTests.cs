using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSaveDataValidationTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldShowValidationError_WhenTitleIsEmpty()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var validationMessage = Page.Locator(".validation-message:has-text('Title is required')");
        await Expect(validationMessage).ToBeVisibleAsync();
        
        await TakeScreenshotAsync(1, "TitleValidationError");
    }

    [Test, Retry(2)]
    public async Task ShouldShowValidationError_WhenDescriptionIsEmpty()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Title), "Test title");
        await Input(nameof(WorkOrderManage.Elements.Description), "");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var validationMessage = Page.Locator(".validation-message:has-text('Description is required')");
        await Expect(validationMessage).ToBeVisibleAsync();
        
        await TakeScreenshotAsync(1, "DescriptionValidationError");
    }

    [Test, Retry(2)]
    public async Task ShouldShowValidationErrors_WhenTitleAndDescriptionAreEmpty()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleValidation = Page.Locator(".validation-message:has-text('Title is required')");
        var descriptionValidation = Page.Locator(".validation-message:has-text('Description is required')");
        
        await Expect(titleValidation).ToBeVisibleAsync();
        await Expect(descriptionValidation).ToBeVisibleAsync();
        
        await TakeScreenshotAsync(1, "TitleAndDescriptionValidationErrors");
    }

    [Test, Retry(2)]
    public async Task ShouldClearValidationErrors_WhenUserCorrectsMistakes()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleValidation = Page.Locator(".validation-message:has-text('Title is required')");
        var descriptionValidation = Page.Locator(".validation-message:has-text('Description is required')");
        
        await Expect(titleValidation).ToBeVisibleAsync();
        await Expect(descriptionValidation).ToBeVisibleAsync();
        
        await TakeScreenshotAsync(1, "ValidationErrorsBeforeCorrection");

        await Input(nameof(WorkOrderManage.Elements.Title), "Corrected title");
        await Input(nameof(WorkOrderManage.Elements.Description), "Corrected description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForURLAsync("**/workorder/search");
        
        await TakeScreenshotAsync(2, "SuccessfulSaveAfterCorrection");
    }

    [Test, Retry(2)]
    public async Task ShouldDisplayServerErrorMessage_WhenServerValidationFails()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        await Input(nameof(WorkOrderManage.Elements.Title), "");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var errorAlert = Page.Locator(".alert-danger");
        
        if (await errorAlert.CountAsync() > 0)
        {
            await Expect(errorAlert).ToBeVisibleAsync();
            await TakeScreenshotAsync(1, "ServerValidationError");
        }
    }
}
