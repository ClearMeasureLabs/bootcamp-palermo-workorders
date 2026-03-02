using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderShelveTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldShelveWorkOrder()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);
        
        order = await ShelveExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ??
            throw new InvalidOperationException();

        rehyratedOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
    }

}