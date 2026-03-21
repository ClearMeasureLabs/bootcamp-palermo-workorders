using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderUnassignTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldUnassignWorkOrderReturningToDraft()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        // Assign the work order
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Input(nameof(WorkOrderManage.Elements.Title), "test title");
        await Input(nameof(WorkOrderManage.Elements.Description), "test description");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify assignment
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var statusLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.Status));
        await Expect(statusLocator).ToHaveTextAsync("Assigned");

        var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
        await Expect(assigneeField).ToBeDisabledAsync();
        await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);

        // Unassign the work order
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + AssignedToDraftCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify unassignment
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await statusLocator.WaitForAsync();
        await Expect(statusLocator).ToHaveTextAsync("Draft");

        var updatedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        updatedOrder.Assignee.ShouldBeNull();
        updatedOrder.AssignedDate.ShouldBeNull();
    }

    [Test, Retry(2)]
    public async Task ShouldNotAllowNonCreatorToUnassign()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        // Get another employee to assign to
        var employees = await Bus.Send(new EmployeeGetAllQuery());
        var otherEmployee = employees.FirstOrDefault(e => e.Id != CurrentUser.Id) ?? throw new InvalidOperationException("No other employee found");

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assign to other employee
        await Select(nameof(WorkOrderManage.Elements.Assignee), otherEmployee.UserName);
        await Input(nameof(WorkOrderManage.Elements.Title), "test title");
        await Input(nameof(WorkOrderManage.Elements.Description), "test description");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Login as the assigned employee (not the creator)
        await Logout();
        await LoginAs(otherEmployee.UserName!);

        // Navigate to the work order
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();

        // Verify that the Unassign button is not available
        var unassignButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.CommandButton) + AssignedToDraftCommand.Name);
        await Expect(unassignButton).ToBeHiddenAsync();
    }
}
