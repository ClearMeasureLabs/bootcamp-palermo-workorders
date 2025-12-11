using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderBeginTests : AcceptanceTestBase
{
    [Test, Retry(3)]
    public async Task ShouldAssignEmployeeAndAssign()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order.Title = "Title from automation";
        order.Description = "Description";
        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + AssignedToInProgressCommand.Name);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title))).ToHaveValueAsync(order.Title!);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description))).ToHaveValueAsync(order.Description!);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee))).ToHaveValueAsync(CurrentUser.UserName);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status))).ToHaveTextAsync(WorkOrderStatus.InProgress.FriendlyName);
    }
}