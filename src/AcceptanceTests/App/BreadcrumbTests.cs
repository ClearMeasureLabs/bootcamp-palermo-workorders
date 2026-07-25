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
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}").WaitForAsync();

        var breadcrumb = Page.GetByTestId(nameof(Breadcrumb.Elements.Breadcrumb));
        await breadcrumb.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await Expect(breadcrumb).ToContainTextAsync("Home");
        await Expect(breadcrumb).ToContainTextAsync("Work Orders");
        await Expect(breadcrumb).ToContainTextAsync("Search");
    }

    [Test, Retry(2)]
    public async Task Should_ClickParentLink_NavigateToWorkOrders()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.GetByTestId(nameof(Counter.Elements.CounterValue)).WaitForAsync();

        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}").WaitForAsync();

        var breadcrumb = Page.GetByTestId(nameof(Breadcrumb.Elements.Breadcrumb));
        await breadcrumb.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        var workOrdersLink = breadcrumb.Locator("a", new LocatorLocatorOptions { HasText = "Work Orders" });
        await workOrdersLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await workOrdersLink.EvaluateAsync("el => el.click()");
        await Page.WaitForURLAsync("**/workorder/search");
        await Expect(Page).ToHaveURLAsync(new Regex("/workorder/search"));
    }

    [Test, Retry(2)]
    public async Task Should_ClickHomeLink_NavigateToHome()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.GetByTestId(nameof(Counter.Elements.CounterValue)).WaitForAsync();

        var breadcrumb = Page.GetByTestId(nameof(Breadcrumb.Elements.Breadcrumb));
        await breadcrumb.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        var homeLink = breadcrumb.Locator("a", new LocatorLocatorOptions { HasText = "Home" });
        await homeLink.EvaluateAsync("el => el.click()");
        await Page.WaitForURLAsync("**/");
        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
    }

    [Test, Retry(2)]
    public async Task Should_NotDisplayBreadcrumb_OnHomePage()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.GetByTestId(nameof(Logout.Elements.WelcomeText)).WaitForAsync();

        await Expect(Page.GetByTestId(nameof(Breadcrumb.Elements.Breadcrumb))).ToHaveCountAsync(0);
    }

    [Test, Retry(2)]
    public async Task Should_DisplayCorrectTrail_OnManageExistingWorkOrder()
    {
        await LoginAsCurrentUser();
        var order = await CreateAndSaveNewWorkOrder();
        await Page.GotoAsync($"/workorder/manage/{order.Number}?mode=Edit");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber)).WaitForAsync();

        var breadcrumb = Page.GetByTestId(nameof(Breadcrumb.Elements.Breadcrumb));
        await breadcrumb.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await Expect(breadcrumb).ToContainTextAsync(order.Number!);
        await Expect(breadcrumb.Locator(".breadcrumb-active")).ToHaveTextAsync(order.Number!);
    }

    [Test, Retry(2)]
    public async Task Should_DisplayNewWorkOrder_InTrail_WhenCreating()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber)).WaitForAsync();

        var breadcrumb = Page.GetByTestId(nameof(Breadcrumb.Elements.Breadcrumb));
        await breadcrumb.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await Expect(breadcrumb).ToContainTextAsync("New Work Order");
        await Expect(breadcrumb.Locator(".breadcrumb-active")).ToHaveTextAsync("New Work Order");
    }
}
