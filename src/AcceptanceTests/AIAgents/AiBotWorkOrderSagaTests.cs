using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.AIAgents;

/// <summary>
///     Acceptance test for the AiBotWorkOrderSaga.
///     Creates a draft work order, assigns it to the AI Bot employee,
///     then waits for the saga to update the description with the AI response
///     and transition the work order to Complete.
/// </summary>
public class AiBotWorkOrderSagaTests : AcceptanceTestBase
{
    [SetUp]
    public async Task EnsurePrerequisites()
    {
        await SkipIfNoChatClient();

        if (!ServerFixture.WorkerStarted)
        {
            Assert.Ignore("Worker is not running — saga cannot execute.");
        }
    }

    [Test, Retry(2), Explicit]
    public async Task ShouldUpdateDescriptionWithAiBotResponse()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        var originalDescription = order.Description;

        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, "aibot");

        WorkOrder? rehydratedOrder = null;

        for (var attempt = 0; attempt < 30; attempt++)
        {
            await Task.Delay(2000);

            rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));

            if (rehydratedOrder?.Status == WorkOrderStatus.Complete)
            {
                break;
            }
        }

        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Status.ShouldBe(WorkOrderStatus.Complete);
        rehydratedOrder.Description.ShouldNotBeNull();
        rehydratedOrder.Description.ShouldContain("AI Bot:");
        rehydratedOrder.Description.ShouldContain(originalDescription!);
    }
}
