using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Services;

[TestFixture]
public class BreadcrumbServiceTests
{
    [Test]
    public void BreadcrumbService_HomeRoute_ShouldHideBreadcrumb()
    {
        using var sut = CreateService("/");

        sut.ShouldShow.ShouldBeFalse();
        sut.Items.ShouldBeEmpty();
    }

    [Test]
    public void BreadcrumbService_LoginRoute_ShouldHideBreadcrumb()
    {
        using var login = CreateService("/login");
        using var healthCheck = CreateService("/_clienthealthcheck");

        login.ShouldShow.ShouldBeFalse();
        healthCheck.ShouldShow.ShouldBeFalse();
    }

    [Test]
    public void BreadcrumbService_CounterRoute_ReturnsHomeCounterTrail()
    {
        using var sut = CreateService("/counter");

        sut.ShouldShow.ShouldBeTrue();
        sut.Items.Count.ShouldBe(2);
        sut.Items[0].Label.ShouldBe("Home");
        sut.Items[0].Url.ShouldBe("/");
        sut.Items[0].IsActive.ShouldBeFalse();
        sut.Items[1].Label.ShouldBe("Counter");
        sut.Items[1].Url.ShouldBeNull();
        sut.Items[1].IsActive.ShouldBeTrue();
    }

    [Test]
    public void BreadcrumbService_FetchDataRoute_ReturnsHomeDataTrail()
    {
        using var sut = CreateService("/fetchdata");

        sut.ShouldShow.ShouldBeTrue();
        sut.Items.Select(i => i.Label).ShouldBe(["Home", "Fetch Data"]);
        sut.Items[0].Url.ShouldBe("/");
        sut.Items[1].IsActive.ShouldBeTrue();
    }

    [Test]
    public void BreadcrumbService_AiAgentRoute_ReturnsHomeAgentTrail()
    {
        using var sut = CreateService("/ai-agent");

        sut.ShouldShow.ShouldBeTrue();
        sut.Items.Select(i => i.Label).ShouldBe(["Home", "AI Agent"]);
    }

    [Test]
    public void BreadcrumbService_SettingsRoute_ReturnsHomeSettingsTrail()
    {
        using var sut = CreateService("/settings");

        sut.ShouldShow.ShouldBeTrue();
        sut.Items.Select(i => i.Label).ShouldBe(["Home", "Settings"]);
    }

    [Test]
    public void BreadcrumbService_WorkOrderSearchRoute_ReturnsSearchTrail()
    {
        using var sut = CreateService("/workorder/search");

        sut.ShouldShow.ShouldBeTrue();
        sut.Items.Count.ShouldBe(3);
        sut.Items[0].Label.ShouldBe("Home");
        sut.Items[1].Label.ShouldBe("Work Orders");
        sut.Items[1].Url.ShouldBe("/workorder/search");
        sut.Items[1].IsActive.ShouldBeFalse();
        sut.Items[2].Label.ShouldBe("Search");
        sut.Items[2].IsActive.ShouldBeTrue();
    }

    [Test]
    public void BreadcrumbService_WorkOrderManageNewRoute_ReturnsNewTrail()
    {
        using var sut = CreateService("/workorder/manage?mode=New");

        sut.ShouldShow.ShouldBeTrue();
        sut.Items.Select(i => i.Label).ShouldBe(["Home", "Work Orders", "New Work Order"]);
        sut.Items[2].IsActive.ShouldBeTrue();
    }

    [Test]
    public void BreadcrumbService_WorkOrderManageExistingRoute_ReturnsWorkOrderTrail()
    {
        using var sut = CreateService("/workorder/manage/WO-123?mode=Edit");

        sut.ShouldShow.ShouldBeTrue();
        sut.Items.Select(i => i.Label).ShouldBe(["Home", "Work Orders", "WO-123"]);
        sut.Items[1].Url.ShouldBe("/workorder/search");
        sut.Items[2].IsActive.ShouldBeTrue();
    }

    [Test]
    public void BreadcrumbService_LocationChanged_UpdatesItems()
    {
        using var ctx = new TestContext();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        using var sut = new BreadcrumbService(navigation);

        sut.ShouldShow.ShouldBeFalse();

        navigation.NavigateTo("/counter");
        sut.ShouldShow.ShouldBeTrue();
        sut.Items[1].Label.ShouldBe("Counter");

        navigation.NavigateTo("/settings");
        sut.Items[1].Label.ShouldBe("Settings");
    }

    private static BreadcrumbService CreateService(string initialUri)
    {
        var navigation = new StubNavigationManager($"https://localhost{initialUri}");
        return new BreadcrumbService(navigation);
    }

    private sealed class StubNavigationManager : NavigationManager
    {
        public StubNavigationManager(string uri)
        {
            Initialize(uri, uri);
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            var absolute = ToAbsoluteUri(uri).ToString();
            Initialize(BaseUri, absolute);
        }
    }
}
