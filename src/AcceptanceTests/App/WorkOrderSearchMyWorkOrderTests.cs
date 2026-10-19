using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class WorkOrderSearchMyWorkOrderTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldShowYouMarker_InSearchRow_WhenWorkOrderAssignedToCurrentUser()
    {
        await LoginAsCurrentUser();

        await Page.GotoAsync(new Uri(new Uri(Page.Url), "/workorder/manage").ToString());
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var title = "MyYouMarker " + Random.Shared.Next(1000, 9999);
        await Input(nameof(WorkOrderManage.Elements.Title), title);
        await Input(nameof(WorkOrderManage.Elements.Description), "Assigned to current user for search highlight");
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        if (!Page.Url.Contains("/workorder/search", StringComparison.OrdinalIgnoreCase))
        {
            await Page.GotoAsync(new Uri(new Uri(Page.Url), "/workorder/search").ToString());
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        var assignedToMe = Page.Locator($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}");
        await assignedToMe.WaitForAsync();
        if (!await assignedToMe.IsCheckedAsync())
        {
            await assignedToMe.CheckAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        var row = Page.Locator("tbody tr", new() { HasTextString = title });
        await row.WaitForAsync(new() { Timeout = 120000 });
        await Expect(row).ToContainTextAsync("(you)");
        var rowClass = await row.GetAttributeAsync("class");
        rowClass.ShouldNotBeNull();
        rowClass.ShouldContain("my-work-order");
    }
}
