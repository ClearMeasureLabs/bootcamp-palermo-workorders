using ClearMeasure.Bootcamp.Core.Model.StateCommands;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[TestFixture]
public class WorkOrderManageTests : AcceptanceTestBase
{
    [Test]
    public async Task WorkOrderFormButtons_AreGreen()
    {
        // Login as current user
        await LoginAsCurrentUser();

        // Create and open new work order form
        var workOrder = await CreateAndSaveNewWorkOrder();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1);

        // Verify "Save" button is green (SaveDraft command)
        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        var saveButton = Page.GetByTestId(saveButtonTestId);
        
        if (await saveButton.CountAsync() > 0)
        {
            await Expect(saveButton).ToBeVisibleAsync();
            var backgroundColor = await saveButton.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundColor");
            backgroundColor.ShouldContain("34, 197, 94"); // RGB for green (#22c55e)
        }

        // Verify "Cancel" button is green (Cancel command if present)
        var cancelButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + CancelCommand.Name;
        var cancelButton = Page.GetByTestId(cancelButtonTestId);
        
        if (await cancelButton.CountAsync() > 0)
        {
            await Expect(cancelButton).ToBeVisibleAsync();
            var backgroundColor = await cancelButton.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundColor");
            backgroundColor.ShouldContain("34, 197, 94"); // RGB for green (#22c55e)
        }

        // Verify "Assign" button is green (Assign command if present)
        var assignButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + AssignCommand.Name;
        var assignButton = Page.GetByTestId(assignButtonTestId);
        
        if (await assignButton.CountAsync() > 0)
        {
            await Expect(assignButton).ToBeVisibleAsync();
            var backgroundColor = await assignButton.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundColor");
            backgroundColor.ShouldContain("34, 197, 94"); // RGB for green (#22c55e)
        }

        await TakeScreenshotAsync(2);
    }
}
