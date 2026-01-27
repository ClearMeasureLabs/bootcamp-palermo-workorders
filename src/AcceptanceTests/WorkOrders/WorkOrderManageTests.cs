using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[TestFixture]
public class WorkOrderManageTests : AcceptanceTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await LoginAsCurrentUser();
    }

    [Test]
    public async Task WorkOrderCreate_AllButtons_DisplayGreenStyling()
    {
        // Arrange: Login and navigate to create work order page
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        // Act: Get the Save button
        var saveButtonTestId = nameof(UI.Shared.Pages.WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        var saveButton = Page.GetByTestId(saveButtonTestId);
        await Expect(saveButton).ToBeVisibleAsync();

        // Assert: Verify Save button has green background color
        var saveButtonBackgroundColor = await saveButton.EvaluateAsync<string>(@"
            element => window.getComputedStyle(element).background
        ");
        
        saveButtonBackgroundColor.ShouldContain("22c55e", Case.Insensitive);
    }

    [Test]
    public async Task WorkOrderEdit_AllButtons_DisplayGreenStyling()
    {
        // Arrange: Create a work order first
        var workOrder = await CreateAndSaveNewWorkOrder();
        await TakeScreenshotAsync(1, "WorkOrderCreated");

        // Navigate to edit page
        await Page.GotoAsync($"/workorder/manage/{workOrder.Number}?mode=Edit");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(2, "EditWorkOrderPage");

        // Act: Get command buttons
        var assignButtonTestId = nameof(UI.Shared.Pages.WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name;
        var assignButton = Page.GetByTestId(assignButtonTestId);
        await Expect(assignButton).ToBeVisibleAsync();

        var cancelButtonTestId = nameof(UI.Shared.Pages.WorkOrderManage.Elements.CommandButton) + AssignedToCancelledCommand.Name;
        var cancelButton = Page.GetByTestId(cancelButtonTestId);
        await Expect(cancelButton).ToBeVisibleAsync();

        // Assert: Verify Assign button has green background color
        var assignButtonBackgroundColor = await assignButton.EvaluateAsync<string>(@"
            element => window.getComputedStyle(element).background
        ");
        
        assignButtonBackgroundColor.ShouldContain("22c55e", Case.Insensitive);

        // Assert: Verify Cancel button has green background color (including btn-red variant)
        var cancelButtonBackgroundColor = await cancelButton.EvaluateAsync<string>(@"
            element => window.getComputedStyle(element).background
        ");
        
        cancelButtonBackgroundColor.ShouldContain("22c55e", Case.Insensitive);
    }
}
