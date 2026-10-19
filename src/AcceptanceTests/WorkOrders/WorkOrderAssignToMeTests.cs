using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderAssignToMeTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldAssignToMeThenAssign_WhenAssignToMeClicked()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await ClickWorkOrderNumberFromSearchPage(order);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        await Click(nameof(WorkOrderManage.Elements.AssignToMe));
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee)))
            .ToHaveValueAsync(CurrentUser.UserName);

        await Input(nameof(WorkOrderManage.Elements.Title), "assigntometitle");
        await Input(nameof(WorkOrderManage.Elements.Description), "assigntomedesc");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        await ClickWorkOrderNumberFromSearchPage(order);

        await woNumberLocator.WaitForAsync();
        var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
        await Expect(assigneeField).ToBeDisabledAsync();
        await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);

        WorkOrder rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!))
            ?? throw new InvalidOperationException();
        rehyratedOrder.Assignee!.UserName.ShouldBe(CurrentUser.UserName);
    }
}
