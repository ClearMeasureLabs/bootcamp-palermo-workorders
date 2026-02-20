using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSaveDataValidationTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldShowValidationErrorWhenTitleIsEmpty()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test Description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
        
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "ValidationErrorTitleEmpty");
        
        var validationMessage = Page.Locator("text=The Title field is required.");
        await Expect(validationMessage).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldShowValidationErrorWhenDescriptionIsEmpty()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Title), "Test Title");
        await Input(nameof(WorkOrderManage.Elements.Description), "");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
        
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "ValidationErrorDescriptionEmpty");
        
        var validationMessage = Page.Locator("text=The Description field is required.");
        await Expect(validationMessage).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldShowValidationErrorsWhenBothTitleAndDescriptionAreEmpty()
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
        await TakeScreenshotAsync(1, "ValidationErrorBothEmpty");
        
        var titleValidationMessage = Page.Locator("text=The Title field is required.");
        await Expect(titleValidationMessage).ToBeVisibleAsync();
        
        var descriptionValidationMessage = Page.Locator("text=The Description field is required.");
        await Expect(descriptionValidationMessage).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldClearValidationErrorsAfterCorrectingFields()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "");
        
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var titleValidationMessage = Page.Locator("text=The Title field is required.");
        await Expect(titleValidationMessage).ToBeVisibleAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "Corrected Title");
        await Input(nameof(WorkOrderManage.Elements.Description), "Corrected Description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
        
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        
        await Page.WaitForURLAsync("**/workorder/search");
        await TakeScreenshotAsync(3, "ValidationClearedAfterCorrection");
    }

    [Test, Retry(2)]
    public async Task ShouldSubmitSuccessfullyWithValidData()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Title), "Valid Title");
        await Input(nameof(WorkOrderManage.Elements.Description), "Valid Description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
        
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        
        await Page.WaitForURLAsync("**/workorder/search");
        await TakeScreenshotAsync(3, "SuccessfulSubmission");
    }
}
