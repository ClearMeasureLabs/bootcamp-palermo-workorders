using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using System.Text.RegularExpressions;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class BreadcrumbTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task Should_DisplayBreadcrumb_OnSearchPage()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var breadcrumb = Page.GetByTestId(nameof(Breadcrumb.Elements.Breadcrumb));
        await Expect(breadcrumb).ToBeVisibleAsync();
        await Expect(breadcrumb).ToContainTextAsync("Home");
        await Expect(breadcrumb).ToContainTextAsync("Work Orders");
        await Expect(breadcrumb).ToContainTextAsync("Search");
    }

    [Test, Retry(2)]
    public async Task Should_ClickParentLink_NavigateToWorkOrders()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var breadcrumb = Page.GetByTestId(nameof(Breadcrumb.Elements.Breadcrumb));
        await breadcrumb.Locator("a", new LocatorLocatorOptions { HasText = "Work Orders" }).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/search");
        await Expect(Page).ToHaveURLAsync(new Regex("/workorder/search"));
    }

    [Test, Retry(2)]
    public async Task Should_ClickHomeLink_NavigateToHome()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var breadcrumb = Page.GetByTestId(nameof(Breadcrumb.Elements.Breadcrumb));
        await breadcrumb.Locator("a", new LocatorLocatorOptions { HasText = "Home" }).ClickAsync();
        await Page.WaitForURLAsync("**/");
        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
    }

    [Test, Retry(2)]
    public async Task Should_NotDisplayBreadcrumb_OnHomePage()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId(nameof(Breadcrumb.Elements.Breadcrumb))).ToHaveCountAsync(0);
    }

    [Test, Retry(2)]
    public async Task Should_DisplayCorrectTrail_OnManageExistingWorkOrder()
    {
        await LoginAsCurrentUser();
        var order = await CreateAndSaveNewWorkOrder();
        await Page.GotoAsync($"/workorder/manage/{order.Number}?mode=Edit");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var breadcrumb = Page.GetByTestId(nameof(Breadcrumb.Elements.Breadcrumb));
        await Expect(breadcrumb).ToBeVisibleAsync();
        await Expect(breadcrumb).ToContainTextAsync(order.Number!);
        await Expect(breadcrumb.Locator(".breadcrumb-active")).ToHaveTextAsync(order.Number!);
    }

    [Test, Retry(2)]
    public async Task Should_DisplayNewWorkOrder_InTrail_WhenCreating()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var breadcrumb = Page.GetByTestId(nameof(Breadcrumb.Elements.Breadcrumb));
        await Expect(breadcrumb).ToBeVisibleAsync();
        await Expect(breadcrumb).ToContainTextAsync("New Work Order");
        await Expect(breadcrumb.Locator(".breadcrumb-active")).ToHaveTextAsync("New Work Order");
    }
}
