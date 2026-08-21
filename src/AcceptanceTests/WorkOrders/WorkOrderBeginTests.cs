using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderBeginTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldAssignEmployeeAndAssign()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order.Title = "Title from automation";
        order.Description = "Description";
        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.InProgress.FriendlyName);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee)))
            .ToHaveValueAsync(CurrentUser.UserName);

        var titleValue = await Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)).InputValueAsync();
        titleValue.ShouldNotBeNullOrWhiteSpace();
        var descriptionValue = await Page.GetByTestId(nameof(WorkOrderManage.Elements.Description)).InputValueAsync();
        descriptionValue.ShouldNotBeNullOrWhiteSpace();
    }
}
