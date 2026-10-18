using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderTitleCharCountTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task TitleCharCount_UpdatesCaption_AsUserTypes()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await WaitForNewWorkOrderFormReadyAsync();

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToBeEditableAsync(new LocatorAssertionsToBeEditableOptions { Timeout = 30_000 });
        await titleField.FillAsync("0123456789");
        await titleField.BlurAsync();

        var caption = Page.GetByTestId(nameof(WorkOrderManage.Elements.TitleCharCount));
        await Expect(caption).ToHaveTextAsync("190 characters remaining");
        await Expect(caption).Not.ToHaveClassAsync(new Regex("text-warning"));
    }
}
