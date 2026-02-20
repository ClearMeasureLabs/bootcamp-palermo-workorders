using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderValidationTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task CreateWorkOrder_WithoutTitle_ShowsValidationError()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        // Fill description but leave title empty
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await TakeScreenshotAsync(2, "FilledDescriptionOnly");

        // Try to save
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(3, "ValidationError");

        // Should still be on the manage page, not redirected
        await Expect(Page).ToHaveURLAsync(new Regex(".*workorder/manage.*"));

        // Should see validation message
        var validationSummary = Page.Locator(".validation-summary");
        await Expect(validationSummary).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task CreateWorkOrder_WithoutDescription_ShowsValidationError()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        // Fill title but leave description empty
        await Input(nameof(WorkOrderManage.Elements.Title), "Test title");
        await TakeScreenshotAsync(2, "FilledTitleOnly");

        // Try to save
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(3, "ValidationError");

        // Should still be on the manage page, not redirected
        await Expect(Page).ToHaveURLAsync(new Regex(".*workorder/manage.*"));

        // Should see validation message
        var validationSummary = Page.Locator(".validation-summary");
        await Expect(validationSummary).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task CreateWorkOrder_WithBothFieldsEmpty_ShowsValidationError()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        // Try to save without filling anything
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(2, "ValidationError");

        // Should still be on the manage page, not redirected
        await Expect(Page).ToHaveURLAsync(new Regex(".*workorder/manage.*"));

        // Should see validation message
        var validationSummary = Page.Locator(".validation-summary");
        await Expect(validationSummary).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task CreateWorkOrder_WithValidData_Succeeds()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        // Should be redirected to search page
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(3, "WorkOrderSearchAfterSave");

        // Should be able to find the work order
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(order.Title!);

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        await Expect(descriptionField).ToHaveValueAsync(order.Description!);
    }
}
