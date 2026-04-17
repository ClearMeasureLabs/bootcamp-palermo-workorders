using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[Parallelizable(ParallelScope.None)]
public class WorkOrderAssignConfirmationTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldShowConfirmationModalWhenAssigningEmployeeWithInProgressWorkOrder()
    {
        await LoginAsCurrentUser();

        var assignee = CurrentUser;
        var inProgressOrder = await CreateInProgressWorkOrderForEmployee(assignee);
        var newOrder = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newOrder.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();

        await Select(nameof(WorkOrderManage.Elements.Assignee), assignee.UserName);
        await Input(nameof(WorkOrderManage.Elements.Title), "Test Order");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        var modal = Page.Locator(".modal-overlay");
        await Expect(modal).ToBeVisibleAsync();

        var modalText = Page.Locator(".modal-body p[role='alert']");
        await Expect(modalText).ToContainTextAsync(inProgressOrder.Number!);
        await Expect(modalText).ToContainTextAsync(inProgressOrder.Title!);
    }

    [Test, Retry(2)]
    public async Task ShouldAssignNewWorkOrderWhenConfirmationIsConfirmed()
    {
        await LoginAsCurrentUser();

        var assignee = CurrentUser;
        var inProgressOrder = await CreateInProgressWorkOrderForEmployee(assignee);
        var newOrder = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newOrder.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();

        await Select(nameof(WorkOrderManage.Elements.Assignee), assignee.UserName);
        await Input(nameof(WorkOrderManage.Elements.Title), "Test Order");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        var confirmButton = Page.Locator(".modal-footer button:has-text('Confirm')");
        await Expect(confirmButton).ToBeVisibleAsync();
        await confirmButton.ClickAsync();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var refreshedOrder = await Bus.Send(new WorkOrderByNumberQuery(newOrder.Number!)) ?? throw new InvalidOperationException();
        refreshedOrder.Assignee?.UserName.ShouldBe(assignee.UserName);
        refreshedOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Test, Retry(2)]
    public async Task ShouldKeepFormStateWhenConfirmationIsCancelled()
    {
        await LoginAsCurrentUser();

        var assignee = CurrentUser;
        var inProgressOrder = await CreateInProgressWorkOrderForEmployee(assignee);
        var newOrder = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newOrder.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();

        await Select(nameof(WorkOrderManage.Elements.Assignee), assignee.UserName);
        await Input(nameof(WorkOrderManage.Elements.Title), "Test Order");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        var cancelButton = Page.Locator(".modal-footer button:has-text('Cancel')");
        await Expect(cancelButton).ToBeVisibleAsync();
        await cancelButton.ClickAsync();

        var modal = Page.Locator(".modal-overlay");
        await Expect(modal).ToBeHiddenAsync();

        var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
        await Expect(assigneeField).ToHaveValueAsync(assignee.UserName);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync("Test Order");

        var refreshedOrder = await Bus.Send(new WorkOrderByNumberQuery(newOrder.Number!)) ?? throw new InvalidOperationException();
        refreshedOrder.Assignee.ShouldBeNull();
        refreshedOrder.Status.ShouldBe(WorkOrderStatus.Draft);
    }

    [Test, Retry(2)]
    public async Task ShouldNotShowConfirmationWhenAssigneeHasNoActiveWorkOrder()
    {
        await LoginAsCurrentUser();

        var assignee = CurrentUser;
        var newOrder = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newOrder.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();

        await Select(nameof(WorkOrderManage.Elements.Assignee), assignee.UserName);
        await Input(nameof(WorkOrderManage.Elements.Title), "Test Order");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var modal = Page.Locator(".modal-overlay");
        await Expect(modal).ToBeHiddenAsync();

        var refreshedOrder = await Bus.Send(new WorkOrderByNumberQuery(newOrder.Number!)) ?? throw new InvalidOperationException();
        refreshedOrder.Assignee?.UserName.ShouldBe(assignee.UserName);
        refreshedOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    private async Task<WorkOrder> CreateInProgressWorkOrderForEmployee(Employee assignee)
    {
        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Select(nameof(WorkOrderManage.Elements.Assignee), assignee.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + AssignedToInProgressCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var refreshedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        return refreshedOrder;
    }
}
