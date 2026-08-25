using System.Globalization;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderDueDateTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldSaveEmptyDueDateAndShowBlankOnSearch()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = $"[{TestTag}] empty due date";
        order.Number = null;
        order.DueDate = null;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        order.Number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);

        var dueDateInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.DueDate));
        await Expect(dueDateInput).ToBeVisibleAsync();
        await Expect(dueDateInput).ToHaveValueAsync(string.Empty);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });

        // Empty <span> has no layout box; Playwright treats that as not visible. Assert attached + blank.
        var dueDateCell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.DueDateCell) + order.Number);
        await Expect(dueDateCell).ToBeAttachedAsync();
        await Expect(dueDateCell).ToHaveTextAsync(string.Empty);

        var rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehydrated.ShouldNotBeNull();
        rehydrated.DueDate.ShouldBeNull();
    }

    [Test, Retry(2)]
    public async Task ShouldPersistDueDateOnManageAndSearch()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = $"[{TestTag}] set due date";
        order.Number = null;
        var dueDate = new DateOnly(2026, 9, 12);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        order.Number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Page.GetByTestId(nameof(WorkOrderManage.Elements.DueDate))
            .FillAsync(dueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });

        var expectedDisplay = dueDate.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
        var dueDateCell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.DueDateCell) + order.Number);
        await Expect(dueDateCell).ToContainTextAsync(expectedDisplay);

        await ClickWorkOrderNumberFromSearchPage(order);
        var dueDateInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.DueDate));
        await Expect(dueDateInput).ToHaveValueAsync(dueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        var rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehydrated!.DueDate.ShouldBe(dueDate);
    }

    [Test, Retry(2)]
    public async Task ShouldColorDueTodayYellowAndOverdueRed_AndRemoveColorWhenCompleteOrCancelled()
    {
        await LoginAsCurrentUser();
        var today = ChurchTimeZone.Today(TimeProvider.System);
        var overdue = today.AddDays(-2);

        var todayOrder = await CreateDraftWithDueDateAsync($"[{TestTag}] due today", today);
        var overdueOrder = await CreateDraftWithDueDateAsync($"[{TestTag}] overdue", overdue);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var todayCell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.DueDateCell) + todayOrder.Number);
        var overdueCell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.DueDateCell) + overdueOrder.Number);
        await Expect(todayCell).ToBeAttachedAsync(new LocatorAssertionsToBeAttachedOptions { Timeout = 30_000 });
        await Expect(overdueCell).ToBeAttachedAsync(new LocatorAssertionsToBeAttachedOptions { Timeout = 30_000 });
        await Expect(todayCell).ToHaveClassAsync(new Regex("due-date-today"));
        await Expect(overdueCell).ToHaveClassAsync(new Regex("due-date-overdue"));

        await CompleteWorkOrderViaBusAsync(todayOrder.Number!);
        await CancelWorkOrderViaBusAsync(overdueOrder.Number!);

        // Full Page.ReloadAsync drops in-memory Blazor auth and lands on /login.
        // Soft-navigate within the SPA so search re-queries without losing the session.
        await Click(nameof(NavMenu.Elements.Counter));
        await Page.WaitForURLAsync("**/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        todayCell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.DueDateCell) + todayOrder.Number);
        overdueCell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.DueDateCell) + overdueOrder.Number);

        await Expect(todayCell).ToBeAttachedAsync(new LocatorAssertionsToBeAttachedOptions { Timeout = 30_000 });
        await Expect(overdueCell).ToBeAttachedAsync(new LocatorAssertionsToBeAttachedOptions { Timeout = 30_000 });
        await Expect(todayCell).Not.ToHaveClassAsync(new Regex("due-date-today|due-date-overdue"));
        await Expect(overdueCell).Not.ToHaveClassAsync(new Regex("due-date-today|due-date-overdue"));
        await Expect(todayCell).ToContainTextAsync(today.ToString("MMM d, yyyy", CultureInfo.InvariantCulture));
        await Expect(overdueCell).ToContainTextAsync(overdue.ToString("MMM d, yyyy", CultureInfo.InvariantCulture));
    }

    private async Task<WorkOrder> CreateDraftWithDueDateAsync(string title, DateOnly dueDate)
    {
        var order = Faker<WorkOrder>();
        order.Title = title;
        order.Number = null;

        var newWorkOrder = Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder));
        await Expect(newWorkOrder).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        order.Number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description ?? "desc");
        var dueDateInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.DueDate));
        await dueDateInput.FillAsync(dueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        var expectedUrgency = DueDateUrgencyCalculator.Calculate(
            dueDate,
            WorkOrderStatus.Draft,
            TimeProvider.System);
        await Expect(dueDateInput).ToHaveClassAsync(
            new Regex(DueDateUrgencyCalculator.CssClass(expectedUrgency)));

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        return order;
    }

    private async Task CompleteWorkOrderViaBusAsync(string number)
    {
        await using var context = TestHost.NewDbContext();
        var workOrder = await context.Set<WorkOrder>().SingleAsync(w => w.Number == number);
        workOrder.Status = WorkOrderStatus.Complete;
        workOrder.CompletedDate = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    private async Task CancelWorkOrderViaBusAsync(string number)
    {
        await using var context = TestHost.NewDbContext();
        var workOrder = await context.Set<WorkOrder>().SingleAsync(w => w.Number == number);
        workOrder.Status = WorkOrderStatus.Cancelled;
        await context.SaveChangesAsync();
    }
}
