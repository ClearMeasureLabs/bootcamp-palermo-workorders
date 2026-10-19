using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderDueDateQuickPickTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldPersistNextSaturdayDueDate_WhenQuickPickClickedAndDraftSaved()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        order.Number.ShouldNotBeNullOrWhiteSpace();

        await ClickWorkOrderNumberFromSearchPage(order);

        await Click(nameof(WorkOrderManage.Elements.DueDateNextSaturday));

        var expected = ComingSaturdayInChicago();
        var dueDateInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.DueDate));
        await Expect(dueDateInput).ToHaveValueAsync(expected.ToString("yyyy-MM-dd"));

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.DueDate)))
            .ToHaveValueAsync(expected.ToString("yyyy-MM-dd"));
    }

    private static DateOnly ComingSaturdayInChicago()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).DateTime);
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
        return today.AddDays(daysUntilSaturday);
    }
}
