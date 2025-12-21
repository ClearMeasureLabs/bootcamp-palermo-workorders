using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderDeadlineTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldCreateWorkOrderWithoutDeadline()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var deadlineDateField = Page.GetByTestId(nameof(WorkOrderManage.Elements.DeadlineDate));
        await Expect(deadlineDateField).ToHaveValueAsync("");

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydratedOrder.Deadline.ShouldBeNull();
        rehydratedOrder.IsOverdue.ShouldBeFalse();
    }

    [Test]
    public async Task ShouldCreateWorkOrderWithFutureDeadline()
    {
        await LoginAsCurrentUser();

        var futureDate = DateTime.Now.AddDays(7);
        var order = await CreateWorkOrderWithDeadline(futureDate);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var deadlineDateField = Page.GetByTestId(nameof(WorkOrderManage.Elements.DeadlineDate));
        await Expect(deadlineDateField).ToHaveValueAsync(futureDate.ToString("yyyy-MM-dd"));

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydratedOrder.Deadline.ShouldNotBeNull();
        rehydratedOrder.IsOverdue.ShouldBeFalse();
    }

    [Test]
    public async Task ShouldShowRedBorderForOverdueWorkOrder()
    {
        await LoginAsCurrentUser();

        var pastDate = DateTime.Now.AddDays(-1);
        var order = await CreateWorkOrderWithDeadline(pastDate);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rowTestId = nameof(WorkOrderSearch.Elements.WorkOrderRow) + order.Number;
        var row = Page.GetByTestId(rowTestId);
        await Expect(row).ToBeVisibleAsync();
        await Expect(row).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("overdue"));

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydratedOrder.Deadline.ShouldNotBeNull();
        rehydratedOrder.IsOverdue.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldNotShowRedBorderForFutureDeadlineWorkOrder()
    {
        await LoginAsCurrentUser();

        var futureDate = DateTime.Now.AddDays(7);
        var order = await CreateWorkOrderWithDeadline(futureDate);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rowTestId = nameof(WorkOrderSearch.Elements.WorkOrderRow) + order.Number;
        var row = Page.GetByTestId(rowTestId);
        await Expect(row).ToBeVisibleAsync();

        var classAttribute = await row.GetAttributeAsync("class") ?? "";
        classAttribute.ShouldNotContain("overdue");

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydratedOrder.IsOverdue.ShouldBeFalse();
    }

    private async Task<WorkOrder> CreateWorkOrderWithDeadline(DateTime deadline)
    {
        var order = Faker<WorkOrder>();
        order.Title = "from automation";
        order.Number = null;
        var testTitle = order.Title;
        var testDescription = order.Description;
        var testRoomNumber = order.RoomNumber;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;
        await Input(nameof(WorkOrderManage.Elements.Title), testTitle);
        await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);

        // Set deadline date
        var deadlineDateLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.DeadlineDate));
        await deadlineDateLocator.FillAsync(deadline.ToString("yyyy-MM-dd"));

        // Set deadline time
        var hour = deadline.Hour % 12;
        if (hour == 0) hour = 12;
        var timeValue = $"{hour:D2}:{deadline.Minute:D2}";
        var deadlineTimeLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.DeadlineTime));
        await deadlineTimeLocator.FillAsync(timeValue);

        // Set AM/PM
        var amPm = deadline.Hour >= 12 ? "PM" : "AM";
        var deadlineAmPmLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.DeadlineAmPm));
        await deadlineAmPmLocator.SelectOptionAsync(amPm);

        await TakeScreenshotAsync(2, "FormFilledWithDeadline");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        if (rehydratedOrder == null)
        {
            await Task.Delay(1000);
            rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        }
        rehydratedOrder.ShouldNotBeNull();

        return rehydratedOrder;
    }
}
