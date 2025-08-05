using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderDeleteDraftTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldDraftWorkOrderAndDelete()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        // click delete button here
        await Click(nameof(WorkOrderSearch.Elements.DeleteWorkOrderButton) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);
        
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Input(nameof(WorkOrderManage.Elements.Title), "newtitle");
        await Input(nameof(WorkOrderManage.Elements.Description), "newdesc");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DeleteDraftCommand.Name);

        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).Not.ToBeVisibleAsync();
    }
}