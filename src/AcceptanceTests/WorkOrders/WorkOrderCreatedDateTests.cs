using System.Globalization;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderCreatedDateTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldDisplayCreatedDateFormattedAsMmDdYyyyOnWorkOrderDetailPage()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = $"[{TestTag}] created date display";
        order.Number = null;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        order.Number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description ?? "desc");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate back to the work order detail page
        await ClickWorkOrderNumberFromSearchPage(order);

        // Verify the CreatedDate field is present and non-empty
        var createdDateLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.CreatedDate));
        await Expect(createdDateLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });

        var createdDateText = await createdDateLocator.TextContentAsync();
        createdDateText.ShouldNotBeNullOrWhiteSpace();

        // Verify the format is MM/dd/yyyy
        var mmDdYyyyPattern = new Regex(@"^\d{2}/\d{2}/\d{4}$");
        mmDdYyyyPattern.IsMatch(createdDateText!.Trim()).ShouldBeTrue(
            $"CreatedDate '{createdDateText}' should be formatted as MM/dd/yyyy");

        // Verify it parses to a valid date matching the persisted CreatedDate
        var rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehydrated.ShouldNotBeNull();
        rehydrated!.CreatedDate.ShouldNotBeNull();

        var displayedDate = DateTime.ParseExact(createdDateText.Trim(), "MM/dd/yyyy", CultureInfo.InvariantCulture);
        displayedDate.Date.ShouldBe(rehydrated.CreatedDate!.Value.Date);
    }
}
