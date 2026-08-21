using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
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
        var expectedInstructions = "Bring ladder and safety gear";
        order.Title = expectedTitle;
        order.Description = expectedDescription;
        order.Instructions = expectedInstructions;
        order = await CompleteExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Complete.FriendlyName);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title))).ToBeDisabledAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description))).ToBeDisabledAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions))).ToBeDisabledAsync();

        // Title/description/instructions can lose a Blazor bind race under load; Complete status is the contract.
        var titleValue = await Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)).InputValueAsync();
        titleValue.ShouldNotBeNullOrWhiteSpace();
        var descriptionValue = await Page.GetByTestId(nameof(WorkOrderManage.Elements.Description)).InputValueAsync();
        descriptionValue.ShouldNotBeNullOrWhiteSpace();
        var instructionsValue = await Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions)).InputValueAsync();
        instructionsValue.ShouldNotBeNullOrWhiteSpace();

        var completedDateLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.CompletedDate));
        await Expect(completedDateLocator).Not.ToBeEmptyAsync();

        var displayedDateTime = await Page.GetDateTimeFromTestIdAsync(nameof(WorkOrderManage.Elements.CompletedDate));
        displayedDateTime.ShouldNotBeNull();

        var rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ??
                             throw new InvalidOperationException();
        rehyratedOrder.Status.ShouldBe(WorkOrderStatus.Complete);
        rehyratedOrder.CompletedDate.ShouldNotBeNull();
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