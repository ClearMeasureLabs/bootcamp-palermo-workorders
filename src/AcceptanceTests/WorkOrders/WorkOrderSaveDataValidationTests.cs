using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSaveDataValidationTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldPreventEmptyTitleSubmission()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "Valid Description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room 101");
        await TakeScreenshotAsync(2, "EmptyTitle");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(3, "ValidationError");

        // Should still be on the same page (not navigated away)
        await Expect(Page).ToHaveURLAsync("**/workorder/manage?mode=New");

        // Should see validation message
        var validationMessage = Page.Locator(".validation-message").Filter(new() { HasText = "Title is required" });
        await Expect(validationMessage).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldPreventEmptyDescriptionSubmission()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "Valid Title");
        await Input(nameof(WorkOrderManage.Elements.Description), "");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room 101");
        await TakeScreenshotAsync(2, "EmptyDescription");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(3, "ValidationError");

        // Should still be on the same page (not navigated away)
        await Expect(Page).ToHaveURLAsync("**/workorder/manage?mode=New");

        // Should see validation message
        var validationMessage = Page.Locator(".validation-message").Filter(new() { HasText = "Description is required" });
        await Expect(validationMessage).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldPreventEmptyTitleAndDescriptionSubmission()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room 101");
        await TakeScreenshotAsync(2, "EmptyTitleAndDescription");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(3, "ValidationErrors");

        // Should still be on the same page (not navigated away)
        await Expect(Page).ToHaveURLAsync("**/workorder/manage?mode=New");

        // Should see validation messages
        var titleValidationMessage = Page.Locator(".validation-message").Filter(new() { HasText = "Title is required" });
        await Expect(titleValidationMessage).ToBeVisibleAsync();

        var descValidationMessage = Page.Locator(".validation-message").Filter(new() { HasText = "Description is required" });
        await Expect(descValidationMessage).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldAllowSubmissionAfterCorrectingValidationErrors()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        // First try with empty fields to trigger validation
        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "Room 101");
        await TakeScreenshotAsync(2, "EmptyFields");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(3, "ValidationErrorsShown");

        // Should still be on the same page
        await Expect(Page).ToHaveURLAsync("**/workorder/manage?mode=New");

        // Now correct the validation errors
        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] Valid Title");
        await Input(nameof(WorkOrderManage.Elements.Description), "Valid Description");
        await TakeScreenshotAsync(4, "CorrectedFields");

        // Submit again
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(5, "SuccessfulSubmission");

        // Should be redirected to search page
        await Page.WaitForURLAsync("**/workorder/search");

        // Verify work order was created by finding it in the list
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newWorkOrderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page).ToHaveURLAsync($"/workorder/manage/{newWorkOrderNumber}?mode=Edit");
    }
}
