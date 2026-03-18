using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.Playwright;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[Parallelizable(ParallelScope.Children)]
public class WorkOrderAssignConfirmationTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldShowConfirmationModal_WhenAssigningEmployeeWithInProgressWorkOrder()
    {
        await LoginAsCurrentUser();

        var assigneeEmployee = await CreateTestEmployeeWithInProgressWorkOrder();
        
        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);
        
        await Select(nameof(WorkOrderManage.Elements.Assignee), assigneeEmployee.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        var modalMessage = Page.Locator(".modal-message");
        await Expect(modalMessage).ToBeVisibleAsync();
        await Expect(modalMessage).ToContainTextAsync(assigneeEmployee.GetFullName());
        await Expect(modalMessage).ToContainTextAsync("currently working on");
    }

    [Test, Retry(2)]
    public async Task ShouldAssignNewWorkOrder_WhenConfirmationIsConfirmed()
    {
        await LoginAsCurrentUser();

        var assigneeEmployee = await CreateTestEmployeeWithInProgressWorkOrder();
        
        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);
        
        await Select(nameof(WorkOrderManage.Elements.Assignee), assigneeEmployee.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        var confirmButton = Page.Locator(".modal-buttons button:has-text('Confirm')");
        await Expect(confirmButton).ToBeVisibleAsync();
        await confirmButton.ClickAsync();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var modal = Page.Locator(".modal-overlay");
        await Expect(modal).ToBeHiddenAsync();

        WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Assignee?.UserName.ShouldBe(assigneeEmployee.UserName);
    }

    [Test, Retry(2)]
    public async Task ShouldKeepFormState_WhenConfirmationIsCancelled()
    {
        await LoginAsCurrentUser();

        var assigneeEmployee = await CreateTestEmployeeWithInProgressWorkOrder();
        
        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);
        
        await Select(nameof(WorkOrderManage.Elements.Assignee), assigneeEmployee.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        var cancelButton = Page.Locator(".modal-buttons button:has-text('Cancel')");
        await Expect(cancelButton).ToBeVisibleAsync();
        await cancelButton.ClickAsync();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var modal = Page.Locator(".modal-overlay");
        await Expect(modal).ToBeHiddenAsync();

        var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
        await Expect(assigneeField).ToHaveValueAsync(assigneeEmployee.UserName);

        WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Assignee.ShouldBeNull();
    }

    [Test, Retry(2)]
    public async Task ShouldNotShowConfirmation_WhenAssigneeHasNoActiveWorkOrder()
    {
        await LoginAsCurrentUser();

        var availableEmployee = await CreateTestEmployeeWithoutInProgressWorkOrder();
        
        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);
        
        await Select(nameof(WorkOrderManage.Elements.Assignee), availableEmployee.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        var modal = Page.Locator(".modal-overlay");
        await Expect(modal).ToBeHiddenAsync();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehydratedOrder.ShouldNotBeNull();
        rehydratedOrder.Assignee?.UserName.ShouldBe(availableEmployee.UserName);
    }

    [Test, Retry(2)]
    public async Task ShouldCloseModal_WhenEscapeKeyIsPressed()
    {
        await LoginAsCurrentUser();

        var assigneeEmployee = await CreateTestEmployeeWithInProgressWorkOrder();
        
        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);
        
        await Select(nameof(WorkOrderManage.Elements.Assignee), assigneeEmployee.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        var modalMessage = Page.Locator(".modal-message");
        await Expect(modalMessage).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var modal = Page.Locator(".modal-overlay");
        await Expect(modal).ToBeHiddenAsync();

        var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
        await Expect(assigneeField).ToHaveValueAsync(assigneeEmployee.UserName);
    }

    private async Task<Employee> CreateTestEmployeeWithInProgressWorkOrder()
    {
        var employee = TestHost.Faker<Employee>();
        employee.UserName = $"test_busy_{TestTag}_{employee.UserName}";
        employee.AddRole(new Role("user", true, true));

        var workOrder = TestHost.Faker<WorkOrder>();
        workOrder.Number = $"WO-INPROG-{TestTag}";
        workOrder.Status = WorkOrderStatus.InProgress;
        workOrder.Assignee = employee;
        workOrder.Creator = CurrentUser;

        using var context = TestHost.NewDbContext();
        context.Add(employee);
        context.Add(workOrder);
        context.SaveChanges();

        return employee;
    }

    private async Task<Employee> CreateTestEmployeeWithoutInProgressWorkOrder()
    {
        var employee = TestHost.Faker<Employee>();
        employee.UserName = $"test_avail_{TestTag}_{employee.UserName}";
        employee.AddRole(new Role("user", true, true));

        using var context = TestHost.NewDbContext();
        context.Add(employee);
        context.SaveChanges();

        return employee;
    }
}
