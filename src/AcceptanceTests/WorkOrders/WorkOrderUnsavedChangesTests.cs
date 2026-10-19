using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderUnsavedChangesTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldShowUnsavedChangesBadge_ThenHideAfterSave()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await ClickWorkOrderNumberFromSearchPage(order);
        await Expect(Page).ToHaveURLAsync(new Regex($"/workorder/manage/{Regex.Escape(order.Number!)}\\?mode=Edit"));

        var badge = Page.GetByTestId(nameof(WorkOrderManage.Elements.UnsavedChangesBadge));
        await Expect(badge).ToHaveCountAsync(0);

        await Input(nameof(WorkOrderManage.Elements.Title), $"{order.Title} - edited");

        await Expect(badge).ToBeVisibleAsync();
        await Expect(badge).ToContainTextAsync("Unsaved changes");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search");
        await Expect(badge).ToHaveCountAsync(0);
    }
}
