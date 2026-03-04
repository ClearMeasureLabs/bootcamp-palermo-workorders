using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderReassignTests : AcceptanceTestBase
{
    private Employee CreateSecondEmployee()
    {
        using var context = TestHost.NewDbContext();
        var employee = Faker<Employee>();
        employee.UserName = $"test_{TestTag}_assignee_{employee.UserName}";
        employee.AddRole(new Role("worker", false, true));
        context.Add(employee);
        context.SaveChanges();
        return employee;
    }

    [Test, Retry(2)]
    public async Task ShouldReassignAssignedWorkOrderToNewEmployee()
    {
        await LoginAsCurrentUser();

        var secondEmployee = CreateSecondEmployee();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.ReassignButton))).ToBeVisibleAsync();

        await Select(nameof(WorkOrderManage.Elements.ReassignAssignee), secondEmployee.UserName);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderManage.Elements.ReassignButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydratedOrder.Assignee?.UserName.ShouldBe(secondEmployee.UserName);
        rehydratedOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Test, Retry(2)]
    public async Task ShouldReassignInProgressWorkOrderAndRevertToAssigned()
    {
        await LoginAsCurrentUser();

        var secondEmployee = CreateSecondEmployee();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.ReassignButton))).ToBeVisibleAsync();

        await Select(nameof(WorkOrderManage.Elements.ReassignAssignee), secondEmployee.UserName);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderManage.Elements.ReassignButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydratedOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
        rehydratedOrder.Assignee?.UserName.ShouldBe(secondEmployee.UserName);
    }
}
