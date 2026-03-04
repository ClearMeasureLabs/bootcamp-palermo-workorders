using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderCostTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldSaveAndDisplayEstimatedCostOnDetailPage()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] cost test");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing estimated cost");
        await Input(nameof(WorkOrderManage.Elements.EstimatedCost), "250.00");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + newWorkOrderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var estimatedCostInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.EstimatedCost));
        await Expect(estimatedCostInput).ToHaveValueAsync("250.00");
    }

    [Test, Retry(2)]
    public async Task ShouldSaveAndDisplayActualCostForInProgressWorkOrder()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title ?? "Updated Title");
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description ?? "Updated Description");
        await Input(nameof(WorkOrderManage.Elements.ActualCost), "310.75");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + InProgressToCompleteCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var actualCostInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.ActualCost));
        await Expect(actualCostInput).ToHaveValueAsync("310.75");
    }

    [Test, Retry(2)]
    public async Task ShouldDisplayCostColumnsOnSearchResultsPage()
    {
        await LoginAsCurrentUser();

        var creator = Faker<Employee>();
        var order = Faker<WorkOrder>();
        order.Creator = creator;
        order.EstimatedCost = 100.00m;
        order.ActualCost = 95.50m;

        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        context.Add(order);
        await context.SaveChangesAsync();

        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderTable = Page.Locator(".grid-data");
        await Expect(workOrderTable).ToBeVisibleAsync();

        var estimatedCostHeader = workOrderTable.Locator("thead th").Filter(new() { HasText = "Estimated Cost" });
        await Expect(estimatedCostHeader).ToHaveCountAsync(1);

        var actualCostHeader = workOrderTable.Locator("thead th").Filter(new() { HasText = "Actual Cost" });
        await Expect(actualCostHeader).ToHaveCountAsync(1);

        var estimatedCostCell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.EstimatedCost) + order.Number);
        await Expect(estimatedCostCell).ToContainTextAsync("100.00");

        var actualCostCell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.ActualCost) + order.Number);
        await Expect(actualCostCell).ToContainTextAsync("95.50");
    }
}
