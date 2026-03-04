using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderCompleteTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldCompleteWorkOrder()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        var expectedTitle = "Title from automation";
        var expectedDescription = "Description";
        order.Title = expectedTitle;
        order.Description = expectedDescription;
        order = await CompleteExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title))).ToHaveValueAsync(expectedTitle,
            new LocatorAssertionsToHaveValueOptions
            {
                Timeout = 10000 // 10 seconds
            });

        // The assignee can reopen a completed work order, so the form remains editable
        // with the Reopen button visible instead of being read-only/disabled.
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title))).ToBeEditableAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description)))
            .ToHaveValueAsync(expectedDescription);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Complete.FriendlyName);

        var reopenButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + CompleteToInProgressCommand.Name;
        await Expect(Page.GetByTestId(reopenButtonTestId)).ToBeVisibleAsync();

        var displayedDateTime = await Page.GetDateTimeFromTestIdAsync(nameof(WorkOrderManage.Elements.CompletedDate));

        var rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ??
                             throw new InvalidOperationException();
        rehyratedOrder.CompletedDate.TruncateToMinute().ShouldBe(displayedDateTime);
    }

    [Test, Retry(2)]
    public async Task CompleteWorkOrderWorkflow()
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

        var rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ??
                             throw new InvalidOperationException();
        rehyratedOrder.Status.ShouldBe(WorkOrderStatus.Complete);

        // The assignee can reopen a completed work order, so the Reopen button
        // is visible instead of a read-only message.
        var reopenButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + CompleteToInProgressCommand.Name;
        await Expect(Page.GetByTestId(reopenButtonTestId)).ToBeVisibleAsync();
    }
}