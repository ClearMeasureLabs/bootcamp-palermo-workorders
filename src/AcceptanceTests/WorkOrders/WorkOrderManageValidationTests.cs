using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderManageValidationTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldShowValidationError_WhenTitleIsEmpty()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        
        await Input(nameof(WorkOrderManage.Elements.Description), "Test Description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
        
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var validationMessage = Page.Locator(".validation-message", new PageLocatorOptions { HasText = "Title" });
        await Expect(validationMessage).ToBeVisibleAsync();
        await Expect(validationMessage).ToContainTextAsync("required");
    }

    [Test, Retry(2)]
    public async Task ShouldShowValidationError_WhenDescriptionIsEmpty()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        
        await Input(nameof(WorkOrderManage.Elements.Title), "Test Title");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
        
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var validationMessage = Page.Locator(".validation-message", new PageLocatorOptions { HasText = "Description" });
        await Expect(validationMessage).ToBeVisibleAsync();
        await Expect(validationMessage).ToContainTextAsync("required");
    }

    [Test, Retry(2)]
    public async Task ShouldShowValidationErrors_WhenBothTitleAndDescriptionAreEmpty()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
        
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var validationSummary = Page.Locator(".validation-summary");
        await Expect(validationSummary).ToBeVisibleAsync();
        
        var titleValidationMessage = Page.Locator(".validation-message", new PageLocatorOptions { HasText = "Title" });
        await Expect(titleValidationMessage).ToBeVisibleAsync();
        
        var descriptionValidationMessage = Page.Locator(".validation-message", new PageLocatorOptions { HasText = "Description" });
        await Expect(descriptionValidationMessage).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldClearValidationErrors_WhenFieldsAreFilled()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var validationSummary = Page.Locator(".validation-summary");
        await Expect(validationSummary).ToBeVisibleAsync();
        
        await Input(nameof(WorkOrderManage.Elements.Title), "Test Title");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test Description");
        
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        
        await Page.WaitForURLAsync("**/workorder/search");
    }
}
