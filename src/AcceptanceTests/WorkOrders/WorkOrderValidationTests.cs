using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderValidationTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldDisplayValidationErrorModalWhenTitleIsEmpty()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Fill in description but leave title empty
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.Title), ""); // Clear title
        
        // Try to save
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Check that modal is visible
        var modal = Page.GetByTestId(nameof(WorkOrderManage.Elements.ValidationModal));
        await Expect(modal).ToBeVisibleAsync();

        // Check that error message contains "Title"
        var errorMessage = Page.GetByTestId(nameof(WorkOrderManage.Elements.ValidationErrorMessage));
        await Expect(errorMessage).ToContainTextAsync("Title");

        await TakeScreenshotAsync(1, "ValidationErrorModal");

        // Close modal
        await Click(nameof(WorkOrderManage.Elements.CloseModalButton));
        await Expect(modal).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task ShouldDisplayValidationErrorModalWhenDescriptionIsEmpty()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Fill in title but leave description empty
        await Input(nameof(WorkOrderManage.Elements.Title), "Test title");
        await Input(nameof(WorkOrderManage.Elements.Description), ""); // Clear description
        
        // Try to save
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Check that modal is visible
        var modal = Page.GetByTestId(nameof(WorkOrderManage.Elements.ValidationModal));
        await Expect(modal).ToBeVisibleAsync();

        // Check that error message contains "Description"
        var errorMessage = Page.GetByTestId(nameof(WorkOrderManage.Elements.ValidationErrorMessage));
        await Expect(errorMessage).ToContainTextAsync("Description");

        await TakeScreenshotAsync(1, "ValidationErrorModalDescription");

        // Close modal
        await Click(nameof(WorkOrderManage.Elements.CloseModalButton));
        await Expect(modal).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task ShouldDisplayBothErrorsWhenTitleAndDescriptionAreEmpty()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Clear both fields
        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "");
        
        // Try to save
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Check that modal is visible
        var modal = Page.GetByTestId(nameof(WorkOrderManage.Elements.ValidationModal));
        await Expect(modal).ToBeVisibleAsync();

        // Check that error message contains both fields
        var errorMessage = Page.GetByTestId(nameof(WorkOrderManage.Elements.ValidationErrorMessage));
        await Expect(errorMessage).ToContainTextAsync("Title");
        await Expect(errorMessage).ToContainTextAsync("Description");

        await TakeScreenshotAsync(1, "ValidationErrorModalBoth");

        // Close modal
        await Click(nameof(WorkOrderManage.Elements.CloseModalButton));
        await Expect(modal).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task ShouldSucceedWhenBothFieldsAreProvided()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Fill in both required fields
        await Input(nameof(WorkOrderManage.Elements.Title), "Valid title");
        await Input(nameof(WorkOrderManage.Elements.Description), "Valid description");
        
        // Save should succeed
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Should navigate to search page
        await Page.WaitForURLAsync("**/workorder/search");

        // Modal should not be visible
        var modal = Page.GetByTestId(nameof(WorkOrderManage.Elements.ValidationModal));
        await Expect(modal).Not.ToBeVisibleAsync();
    }
}
