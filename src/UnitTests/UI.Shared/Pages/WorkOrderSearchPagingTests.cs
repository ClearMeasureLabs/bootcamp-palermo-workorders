using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using Bunit;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderSearchPagingTests
{
    private const string CurrentUsername = "jpalermo";

    private static BunitContext CreateContext(IBus? bus = null, string loggedInAs = CurrentUsername)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton(bus ?? new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton(new WorkOrderSearchState());
        var store = new StubUserSessionStore { Username = loggedInAs };
        var authProvider = new CustomAuthenticationStateProvider(store);
        authProvider.Login(loggedInAs).GetAwaiter().GetResult();
        ctx.Services.AddSingleton<AuthenticationStateProvider>(authProvider);
        return ctx;
    }

    private static WorkOrder[] CreateWorkOrders(int count)
    {
        var orders = new WorkOrder[count];
        for (var i = 0; i < count; i++)
        {
            orders[i] = new WorkOrder
            {
                Number = $"WO{i:D4}",
                Title = $"Title {i:D3}",
                Status = WorkOrderStatus.Draft,
                Creator = new Employee(CurrentUsername, "Jeff", "Palermo", "jeff@example.com")
            };
        }

        return orders;
    }

    [Test]
    public void ShouldShowTwentyFiveResultsPerPage_WhenMoreThanTwentyFiveExist()
    {
        using var ctx = CreateContext(new StubBus(CreateWorkOrders(30)));
        var cut = ctx.Render<WorkOrderSearch>();

        cut.FindAll("table.grid-data tbody tr").Count.ShouldBe(25);
        cut.Find("[data-testid='SearchPagerLabel']").TextContent.ShouldContain("Page 1 of 2");
        cut.Markup.ShouldContain("Search Results (30 work orders)");
    }

    [Test]
    public void ShouldHidePager_WhenTwentyFiveOrFewerResults()
    {
        using var ctx = CreateContext(new StubBus(CreateWorkOrders(25)));
        var cut = ctx.Render<WorkOrderSearch>();

        cut.FindAll("table.grid-data tbody tr").Count.ShouldBe(25);
        cut.FindAll("[data-testid='SearchPager']").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldDisablePreviousOnFirstPage_AndNextOnLastPage()
    {
        await using var ctx = CreateContext(new StubBus(CreateWorkOrders(30)));
        var cut = ctx.Render<WorkOrderSearch>();

        var previous = cut.Find("[data-testid='SearchPagerPrevious']");
        var next = cut.Find("[data-testid='SearchPagerNext']");
        previous.HasAttribute("disabled").ShouldBeTrue();
        next.HasAttribute("disabled").ShouldBeFalse();

        await next.ClickAsync();

        previous = cut.Find("[data-testid='SearchPagerPrevious']");
        next = cut.Find("[data-testid='SearchPagerNext']");
        previous.HasAttribute("disabled").ShouldBeFalse();
        next.HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='SearchPagerLabel']").TextContent.ShouldContain("Page 2 of 2");
        cut.FindAll("table.grid-data tbody tr").Count.ShouldBe(5);
    }

    [Test]
    public async Task ShouldResetToPageOne_WhenSorting()
    {
        await using var ctx = CreateContext(new StubBus(CreateWorkOrders(30)));
        var cut = ctx.Render<WorkOrderSearch>();

        await cut.Find("[data-testid='SearchPagerNext']").ClickAsync();
        cut.Find("[data-testid='SearchPagerLabel']").TextContent.ShouldContain("Page 2 of 2");

        await cut.Find($"#{WorkOrderSearch.Elements.SortByTitleButton}").ClickAsync();

        cut.Find("[data-testid='SearchPagerLabel']").TextContent.ShouldContain("Page 1 of 2");
        cut.FindAll("table.grid-data tbody tr").Count.ShouldBe(25);
    }
}
