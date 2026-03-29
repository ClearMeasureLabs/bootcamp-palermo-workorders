using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderReassignTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldReassignCompletedWorkOrderToSameAssignee()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await CompleteExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Complete.FriendlyName);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + CompleteToAssignedCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Assigned.FriendlyName);

        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!))
            ?? throw new InvalidOperationException();
        rehydratedOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
        rehydratedOrder.CompletedDate.ShouldBeNull();
        rehydratedOrder.AssignedDate.ShouldNotBeNull();
    }

    [Test, Retry(2)]
    public async Task ShouldReassignCompletedWorkOrderToDifferentAssignee()
    {
        await LoginAsCurrentUser();

        var secondEmployee = CreateSecondEmployee();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await CompleteExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Complete.FriendlyName);

        await Select(nameof(WorkOrderManage.Elements.Assignee), secondEmployee.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + CompleteToAssignedCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Assigned.FriendlyName);

        var rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!))
            ?? throw new InvalidOperationException();
        rehydratedOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
        rehydratedOrder.CompletedDate.ShouldBeNull();
        rehydratedOrder.AssignedDate.ShouldNotBeNull();
        rehydratedOrder.Assignee!.UserName.ShouldBe(secondEmployee.UserName);
    }

    [Test, Retry(2)]
    public async Task NonCreatorShouldNotSeeReassignButtonOnCompletedWorkOrder()
    {
        await LoginAsCurrentUser();

        var secondEmployee = CreateSecondEmployee();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, secondEmployee.UserName);

        CurrentUser = secondEmployee;
        await Page.GotoAsync("/");
        await LoginAsCurrentUser();

        order = await ClickWorkOrderNumberFromSearchPage(order);
        await BeginWorkOrderAsAssignee(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);
        await CompleteWorkOrderAsAssignee(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Complete.FriendlyName);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.ReadOnlyMessage)))
            .ToHaveTextAsync("This work order is read-only for you at this time.");

        var reassignButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.CommandButton) + CompleteToAssignedCommand.Name);
        await Expect(reassignButton).Not.ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ReassignButtonShouldOnlyBeVisibleOnCompleteStatus()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Draft.FriendlyName);
        var reassignButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.CommandButton) + CompleteToAssignedCommand.Name);
        await Expect(reassignButton).Not.ToBeVisibleAsync();

        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Assigned.FriendlyName);
        reassignButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.CommandButton) + CompleteToAssignedCommand.Name);
        await Expect(reassignButton).Not.ToBeVisibleAsync();

        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.InProgress.FriendlyName);
        reassignButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.CommandButton) + CompleteToAssignedCommand.Name);
        await Expect(reassignButton).Not.ToBeVisibleAsync();
    }

    private Employee CreateSecondEmployee()
    {
        using var context = IntegrationTests.TestHost.NewDbContext();
        var employee = Faker<Employee>();
        employee.UserName = $"test_{TestTag}_second_{employee.UserName}";
        employee.AddRole(new Role("admin", true, true));
        context.Add(employee);
        context.SaveChanges();
        return employee;
    }

    private async Task BeginWorkOrderAsAssignee(WorkOrder order)
    {
        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + AssignedToInProgressCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task CompleteWorkOrderAsAssignee(WorkOrder order)
    {
        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + InProgressToCompleteCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
