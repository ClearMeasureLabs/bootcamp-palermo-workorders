using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.AIAgents;

/// <summary>
/// Copilot prompt
/// create a test in   that uses playwright to log in as
/// timothy lovejoy and create a draft work order assigned
/// to Homer Simpson with the title and test of description.
/// Then assign it. Then wait 6 seconds. Then click on the
/// number in the table of the search screen and verify
/// that the status is cancelled
/// </summary>
public class AutoCancelAgentTests : AcceptanceTestBase
{
    [Test, Retry(2), Explicit]
    public async Task ShouldAutoCancelWorkOrderWithTestKeywords()
    {
        // [TO20251120] This test relies an AI agent to runn
        // Set up specific users for this test
        var timothyLovejoy = await Bus.Send(new EmployeeByUserNameQuery("tlovejoy"));
        var homerSimpson = await Bus.Send(new EmployeeByUserNameQuery("hsimpson"));
        
        timothyLovejoy.ShouldNotBeNull();
        homerSimpson.ShouldNotBeNull();
        
        // Override CurrentUser to be Timothy Lovejoy for this test
        CurrentUser = timothyLovejoy;
        
        await LoginAsCurrentUser();

        // Create a new work order
        var order = await CreateAndSaveNewWorkOrder();
        order.Title = "test demo";
        order = await ClickWorkOrderNumberFromSearchPage(order);

        // Assign the work order to Homer Simpson with test keywords
        order.Description += " test demo temporary";
        await AssignExistingWorkOrder(order, homerSimpson.UserName);

        // Click on work order again to verify it was auto-cancelled
        async Task<WorkOrder> DelayedTask()
        {
            // Wait 6 seconds for the background AI agent service to process and cancel
            await Task.Delay(6000);
            return await ClickWorkOrderNumberFromSearchPage(order);
        }

        order = await DelayedTask();
        
        // Verify the work order status is now cancelled
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Cancelled.FriendlyName);
    }

}
