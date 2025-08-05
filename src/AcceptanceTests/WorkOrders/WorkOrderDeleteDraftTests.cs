using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderDeleteDraftTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldDeleteDraftWorkOrder()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Delete");
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var deletedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        deletedOrder.ShouldBeNull();
    }

    //todo do we need a test for a draft can't be deleted by a user not the creator?
}