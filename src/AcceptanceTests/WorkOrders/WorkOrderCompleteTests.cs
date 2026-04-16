using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model;
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
        var expectedTitle = order.Title ?? "";
        var expectedDescription = order.Description ?? "";
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        var expectedInstructions = "Finish before end of day";
        order.Title = expectedTitle;
        order.Description = expectedDescription;
        order.Instructions = expectedInstructions;
        await Input(nameof(WorkOrderManage.Elements.Instructions), expectedInstructions);
        order = await CompleteExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title))).ToHaveValueAsync(expectedTitle,
            new LocatorAssertionsToHaveValueOptions
            {
                Timeout = 10000 // 10 seconds
            });

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title))).ToBeDisabledAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description)))
            .ToHaveValueAsync(expectedDescription);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description))).ToBeDisabledAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions)))
            .ToHaveValueAsync(expectedInstructions);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions))).ToBeDisabledAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Complete.FriendlyName);


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

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.ReadOnlyMessage)))
            .ToHaveTextAsync("This work order is read-only for you at this time.");
    }
}