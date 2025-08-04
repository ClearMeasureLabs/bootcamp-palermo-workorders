using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderShelveTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldAssignBeginAndShelve()
    {
        await LoginAsCurrentUser();

        // create
        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);

        //assign
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        // in progress
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + 
                    AssignedToInProgressCommand.Name);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        // shelf
        await Click(nameof(WorkOrderManage.Elements.CommandButton) +
                    InProgressToAssignedCommand.Name);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Assigned.FriendlyName);

    }
}