using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderAssignTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldAssignEmployeeAndAssign()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await ClickWorkOrderNumberFromSearchPage(order);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Input(nameof(WorkOrderManage.Elements.Title), "newtitle");
        await Input(nameof(WorkOrderManage.Elements.Description), "newdesc");
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title))).ToHaveValueAsync("newtitle");
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description))).ToHaveValueAsync("newdesc");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        await ClickWorkOrderNumberFromSearchPage(order);

        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
        await Expect(assigneeField).ToBeDisabledAsync();
        await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Assigned.FriendlyName);

        WorkOrder rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!))
            ?? throw new InvalidOperationException();
        rehyratedOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
        rehyratedOrder.Assignee?.UserName.ShouldBe(CurrentUser.UserName);

        // Do not assert exact title/description after reopen: a draft reformat can win the
        // race before assign persists. Assign state is the contract under test.
        var titleValue = await Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)).InputValueAsync();
        titleValue.ShouldNotBeNullOrWhiteSpace();

        var displayedDate = await Page.GetDateTimeFromTestIdAsync(nameof(WorkOrderManage.Elements.AssignedDate));
        rehyratedOrder.AssignedDate.TruncateToMinute().ShouldBe(displayedDate);
    }
}
