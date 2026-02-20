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

        await Input(nameof(WorkOrderManage.Elements.Description), "Valid Description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room 101");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var validationMessage = Page.Locator(".validation-message:has-text('Title is required')");
        await Expect(validationMessage).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldShowValidationError_WhenDescriptionIsEmpty()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Title), "Valid Title");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room 101");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var validationMessage = Page.Locator(".validation-message:has-text('Description is required')");
        await Expect(validationMessage).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldShowValidationErrors_WhenBothTitleAndDescriptionAreEmpty()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room 101");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleValidationMessage = Page.Locator(".validation-message:has-text('Title is required')");
        await Expect(titleValidationMessage).ToBeVisibleAsync();

        var descriptionValidationMessage = Page.Locator(".validation-message:has-text('Description is required')");
        await Expect(descriptionValidationMessage).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldClearValidationErrors_WhenUserEntersValidData()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room 101");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleValidationMessage = Page.Locator(".validation-message:has-text('Title is required')");
        await Expect(titleValidationMessage).ToBeVisibleAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "Valid Title");
        await Input(nameof(WorkOrderManage.Elements.Description), "Valid Description");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Test, Retry(2)]
    public async Task ShouldShowServerError_WhenServerValidationFails()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.EvaluateAsync(@"
            const titleInput = document.querySelector('[data-testid=""Title""]');
            const descInput = document.querySelector('[data-testid=""Description""]');
            titleInput.removeAttribute('required');
            descInput.removeAttribute('required');
            titleInput.value = '';
            descInput.value = '';
            titleInput.dispatchEvent(new Event('change', { bubbles: true }));
            descInput.dispatchEvent(new Event('change', { bubbles: true }));
        ");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var alertMessage = Page.Locator(".alert-danger");
        await Expect(alertMessage).ToBeVisibleAsync();
        var alertText = await alertMessage.TextContentAsync();
        alertText.ShouldContain("required");
    }
}
