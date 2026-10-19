using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class WorkOrderSearchMyWorkOrderTests : AcceptanceTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await LoginAsCurrentUser();
    }

    [Test, Retry(2)]
    public async Task ShouldShowYouMarker_InSearchRow_WhenWorkOrderAssignedToCurrentUser()
    {
        var myOrder = Faker<WorkOrder>();
        var otherOrder = Faker<WorkOrder>();
        var otherEmployee = Faker<Employee>();
        myOrder.Creator = CurrentUser;
        myOrder.Assignee = CurrentUser;
        myOrder.Title = $"[{TestTag}] my you-marker order";
        otherOrder.Creator = otherEmployee;
        otherOrder.Assignee = otherEmployee;
        otherOrder.Title = $"[{TestTag}] other you-marker order";

        await using var context = TestHost.NewDbContext();
        context.Attach(CurrentUser);
        context.Add(otherEmployee);
        context.Add(myOrder);
        context.Add(otherOrder);
        await context.SaveChangesAsync();

        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var myRow = Page.Locator("tbody tr").Filter(new() { HasText = myOrder.Title });
        await Expect(myRow).ToBeVisibleAsync();
        await Expect(myRow).ToContainTextAsync("(you)");
        var myRowClass = await myRow.GetAttributeAsync("class");
        myRowClass.ShouldNotBeNull();
        myRowClass.ShouldContain("my-work-order");

        var otherRow = Page.Locator("tbody tr").Filter(new() { HasText = otherOrder.Title });
        await Expect(otherRow).ToBeVisibleAsync();
        await Expect(otherRow).Not.ToContainTextAsync("(you)");
        var otherRowClass = await otherRow.GetAttributeAsync("class") ?? "";
        otherRowClass.ShouldNotContain("my-work-order");
    }
}
