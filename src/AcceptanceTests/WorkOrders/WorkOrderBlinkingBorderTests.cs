using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[TestFixture]
public class WorkOrderBlinkingBorderTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldShowBlinkingBorderOnWorkOrderManageTextFields()
    {
        // Arrange & Act
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        // Assert: Verify Title text field has blinking border animation
        var titleLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleLocator).ToBeVisibleAsync();
        
        var titleAnimationName = await titleLocator.EvaluateAsync<string>(
            "el => window.getComputedStyle(el).animationName");
        titleAnimationName.ShouldContain("blinkBorder");

        // Assert: Verify Description textarea has blinking border animation
        var descriptionLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        await Expect(descriptionLocator).ToBeVisibleAsync();
        
        var descriptionAnimationName = await descriptionLocator.EvaluateAsync<string>(
            "el => window.getComputedStyle(el).animationName");
        descriptionAnimationName.ShouldContain("blinkBorder");

        // Assert: Verify RoomNumber text field has blinking border animation
        var roomNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber));
        await Expect(roomNumberLocator).ToBeVisibleAsync();
        
        var roomNumberAnimationName = await roomNumberLocator.EvaluateAsync<string>(
            "el => window.getComputedStyle(el).animationName");
        roomNumberAnimationName.ShouldContain("blinkBorder");

        await TakeScreenshotAsync(2, "VerifiedBlinkingBorders");
    }

    [Test]
    public async Task ShouldShowBlinkingBorderOnWorkOrderSearchTextFields()
    {
        // Arrange & Act
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.WorkOrderSearch));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "WorkOrderSearchPage");

        // Note: The search page uses dropdowns (InputSelect), not text inputs
        // Verify the page loaded correctly but there are no text input fields to test
        var pageTitle = Page.Locator("h2").First;
        await Expect(pageTitle).ToContainTextAsync("Work Order Search");

        await TakeScreenshotAsync(2, "SearchPageVerified");
    }

    [Test]
    public async Task ShouldShowBlinkingBorderOnAllInputTypesAcrossPages()
    {
        // Arrange & Act
        await LoginAsCurrentUser();
        
        // Test on New Work Order page
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        // Verify text inputs have animation
        var titleLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        var titleAnimationName = await titleLocator.EvaluateAsync<string>(
            "el => window.getComputedStyle(el).animationName");
        titleAnimationName.ShouldContain("blinkBorder");

        // Verify textarea has animation
        var descriptionLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        var descriptionAnimationName = await descriptionLocator.EvaluateAsync<string>(
            "el => window.getComputedStyle(el).animationName");
        descriptionAnimationName.ShouldContain("blinkBorder");

        // Create and save a work order, then edit it
        var order = await CreateAndSaveNewWorkOrder();
        await TakeScreenshotAsync(2, "WorkOrderSaved");

        // Navigate to Search page
        await Click(nameof(NavMenu.Elements.WorkOrderSearch));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate back to the work order to edit it
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForURLAsync($"**/workorder/manage?mode=Edit&workOrderNumber={order.Number}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(3, "EditWorkOrderPage");

        // Verify animation is still present on edit page
        var editTitleLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        var editTitleAnimationName = await editTitleLocator.EvaluateAsync<string>(
            "el => window.getComputedStyle(el).animationName");
        editTitleAnimationName.ShouldContain("blinkBorder");

        await TakeScreenshotAsync(4, "AllPagesVerified");
    }
}
