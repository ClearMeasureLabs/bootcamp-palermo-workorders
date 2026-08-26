using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

public class WorkOrderSearchTests
{
    [Test]
    public async Task ShouldRenderMockStatusPillsWithoutVisibleSearchForm()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();
        var pills = component.FindAll(".filter-pill");

        pills.Count.ShouldBe(6);
        pills.Select(pill => pill.TextContent.Trim()).ShouldBe(
        [
            "All",
            "Due Today",
            "Due This Week",
            "In Progress",
            "On Hold",
            "Completed"
        ]);
        pills[0].ClassList.ShouldContain("active");
        component.FindAll(".filters-grid").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldMapStatusPillsToExistingStatusQueries()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        component.Find($"[data-testid='{WorkOrderSearch.Elements.InProgressFilter}']")
            .GetAttribute("href").ShouldBe("/workorder/search?Status=InProgress");
        component.Find($"[data-testid='{WorkOrderSearch.Elements.OnHoldFilter}']")
            .GetAttribute("href").ShouldBe("/workorder/search?Status=Assigned");
        component.Find($"[data-testid='{WorkOrderSearch.Elements.CompletedFilter}']")
            .GetAttribute("href").ShouldBe("/workorder/search?Status=Complete");
        component.Find($"[data-testid='{WorkOrderSearch.Elements.DueTodayFilter}']")
            .GetAttribute("href").ShouldBe("/workorder/search?View=DueToday");
        component.Find($"[data-testid='{WorkOrderSearch.Elements.DueThisWeekFilter}']")
            .GetAttribute("href").ShouldBe("/workorder/search?View=DueThisWeek");
    }

    [Test]
    public async Task ShouldLoadWorkOrderStackWithAllFiltersSetToAllOnInitialLoad()
    {
        await using var ctx = new BunitContext();

        // Arrange
        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        // Act
        var component = ctx.Render<WorkOrderSearch>();

        // Assert
        var workOrderStack = component.Find(".work-order-stack");
        workOrderStack.ShouldNotBeNull();

        var workOrderCards = workOrderStack.QuerySelectorAll(".work-order-card");
        workOrderCards.Length.ShouldBe(2);
    }

    [Test]
    public async Task ShouldLoadWorkOrderStackWithCreatorFilterOnInitialLoad()
    {
        await using var ctx = new BunitContext();

        // Arrange
        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Creator", "somename");
        navigationManager.NavigateTo(uri);

        // Act
        var component = ctx.Render<WorkOrderSearch>();

        // Assert
        var workOrderStack = component.Find(".work-order-stack");
        var workOrderCards = workOrderStack.QuerySelectorAll(".work-order-card");
        workOrderCards.Length.ShouldBe(2);
    }

    [Test]
    public async Task ShouldLoadWorkOrderStackWithAssigneeFilterOnInitialLoad()
    {
        await using var ctx = new BunitContext();

        // Arrange
        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Assignee", "somename");
        navigationManager.NavigateTo(uri);

        // Act
        var component = ctx.Render<WorkOrderSearch>();

        // Assert
        var workOrderStack = component.Find(".work-order-stack");
        var workOrderCards = workOrderStack.QuerySelectorAll(".work-order-card");
        workOrderCards.Length.ShouldBe(2);
    }

    [Test]
    public async Task ShouldLoadWorkOrderStackWithStatusFilterOnInitialLoad()
    {
        await using var ctx = new BunitContext();

        // Arrange
        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Status", WorkOrderStatus.Assigned.Key);
        navigationManager.NavigateTo(uri);

        // Act
        var component = ctx.Render<WorkOrderSearch>();

        // Assert
        var workOrderStack = component.Find(".work-order-stack");
        var workOrderCards = workOrderStack.QuerySelectorAll(".work-order-card");
        workOrderCards.Length.ShouldBe(2);
    }

    [Test]
    public async Task ShouldKeepLegacyFilterControlsOffCanvas()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        component.Find(".legacy-search-controls").GetAttribute("aria-hidden").ShouldBe("true");
        component.FindAll(".search-filters-card").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldSelectCardWithoutOpeningFocusPane()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();
        var cards = component.FindAll(".work-order-card");

        await cards[0].ClickAsync(new());

        cards = component.FindAll(".work-order-card");
        cards[0].ClassList.ShouldContain("selected");
        cards[0].GetAttribute("aria-selected").ShouldBe("true");
        component.FindAll(".focus-pane").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldExposeWorkOrderNumberAsManageLinkText()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();
        var manageLinks = component.FindAll(".manage-link");

        manageLinks.Count.ShouldBe(2);
        foreach (var link in manageLinks)
        {
            var numberSpan = link.QuerySelector(".work-order-number").ShouldNotBeNull();
            var number = numberSpan.TextContent.Trim();
            number.ShouldNotBeNullOrWhiteSpace();
            link.TextContent.Trim().ShouldContain(number);
            link.GetAttribute("data-testid").ShouldBe(WorkOrderSearch.Elements.WorkOrderLink + number);
        }
    }
}
