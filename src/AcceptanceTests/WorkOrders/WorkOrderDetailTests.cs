using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[TestFixture]
public class WorkOrderDetailTests : AcceptanceTestBase
{
    [Test]
    public async Task WorkOrderDetailButtons_AreGreen()
    {
        // Login as current user
        await LoginAsCurrentUser();

        // Navigate to existing work order detail page
        var workOrder = await CreateAndSaveNewWorkOrder();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1);

        // Check all available action buttons
        var commandButtons = new[]
        {
            SaveDraftCommand.Name,
            AssignCommand.Name,
            BeginCommand.Name,
            CompleteCommand.Name,
            CancelCommand.Name,
            ShelveCommand.Name
        };

        foreach (var command in commandButtons)
        {
            var buttonTestId = nameof(WorkOrderManage.Elements.CommandButton) + command;
            var button = Page.GetByTestId(buttonTestId);

            if (await button.CountAsync() > 0)
            {
                await Expect(button).ToBeVisibleAsync();
                var backgroundColor = await button.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundColor");
                backgroundColor.ShouldContain("34, 197, 94"); // RGB for green (#22c55e)
            }
        }

        await TakeScreenshotAsync(2);
    }
}
