using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[TestFixture]
public class WorkOrderManageTests : AcceptanceTestBase
{
    [Test]
    public async Task WorkOrderForm_TextInputs_HaveBlinkingBorders()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        var descriptionInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        var roomInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber));

        var titleAnimation = await titleInput.EvaluateAsync<string>("element => window.getComputedStyle(element).animationName");
        var descriptionAnimation = await descriptionInput.EvaluateAsync<string>("element => window.getComputedStyle(element).animationName");
        var roomAnimation = await roomInput.EvaluateAsync<string>("element => window.getComputedStyle(element).animationName");

        titleAnimation.ShouldBe("blinking-border");
        descriptionAnimation.ShouldBe("blinking-border");
        roomAnimation.ShouldBe("blinking-border");
    }

    [Test]
    public async Task WorkOrderEditForm_TextInputs_HaveBlinkingBorders()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        var descriptionInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        var roomInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber));

        var titleAnimation = await titleInput.EvaluateAsync<string>("element => window.getComputedStyle(element).animationName");
        var descriptionAnimation = await descriptionInput.EvaluateAsync<string>("element => window.getComputedStyle(element).animationName");
        var roomAnimation = await roomInput.EvaluateAsync<string>("element => window.getComputedStyle(element).animationName");

        titleAnimation.ShouldBe("blinking-border");
        descriptionAnimation.ShouldBe("blinking-border");
        roomAnimation.ShouldBe("blinking-border");
    }

    [Test]
    public async Task WorkOrderSearchForm_TextInput_HasBlinkingBorder()
    {
        await LoginAsCurrentUser();
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // WorkOrderSearch only has dropdown selects (InputSelect), not text inputs
        // So we verify that no text input exists on this page
        var textInputs = await Page.Locator(".input-text, .input-textarea").CountAsync();
        textInputs.ShouldBe(0);
    }
}
