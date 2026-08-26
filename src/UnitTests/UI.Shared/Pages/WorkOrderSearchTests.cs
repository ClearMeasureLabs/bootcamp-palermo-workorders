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
    public async Task ShouldLoadDropDownsInitiallyOnLoad()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        var creatorSelect = component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = component.Find($"#{WorkOrderSearch.Elements.StatusSelect}");

        creatorSelect.ShouldNotBeNull();
        assigneeSelect.ShouldNotBeNull();
        statusSelect.ShouldNotBeNull();

        var creatorOptions = creatorSelect.QuerySelectorAll("option");
        creatorOptions.Length.ShouldBe(6);
        creatorOptions[0].TextContent.ShouldBe("All");

        var assigneeOptions = assigneeSelect.QuerySelectorAll("option");
        assigneeOptions.Length.ShouldBe(6);
        assigneeOptions[0].TextContent.ShouldBe("All");

        var statusOptions = statusSelect.QuerySelectorAll("option");
        statusOptions.Length.ShouldBe(6);
        statusOptions[0].TextContent.ShouldBe("All");
    }

    [Test]
    public async Task ShouldAssociateFilterLabelsWithMatchingSelectIds()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        AssertLabelForMatchesSelectId(component, WorkOrderSearch.Elements.CreatorSelect);
        AssertLabelForMatchesSelectId(component, WorkOrderSearch.Elements.AssigneeSelect);
        AssertLabelForMatchesSelectId(component, WorkOrderSearch.Elements.StatusSelect);
    }

    private static void AssertLabelForMatchesSelectId(IRenderedComponent<WorkOrderSearch> component, WorkOrderSearch.Elements element)
    {
        var id = element.ToString();
        var select = component.Find($"#{id}");
        select.ShouldNotBeNull();
        var label = component.Find($"label[for='{id}']");
        label.ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldLoadWorkOrderDeckWithAllFiltersSetToAllOnInitialLoad()
    {
        await using var ctx = new BunitContext();

        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        component.FindAll("table.grid-data").Count.ShouldBe(0);
        var deck = component.Find($"[data-testid='{WorkOrderSearch.Elements.ResultsDeck}']");
        deck.ShouldNotBeNull();

        var cards = component.FindAll(".deck-card");
        cards.Count.ShouldBe(2);
    }

    [Test]
    public async Task ShouldLoadWorkOrderDeckWithCreatorFilterOnInitialLoad()
    {
        await using var ctx = new BunitContext();

        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Creator", "somename");
        navigationManager.NavigateTo(uri);

        var component = ctx.Render<WorkOrderSearch>();

        var cards = component.FindAll(".deck-card");
        cards.Count.ShouldBe(2);
    }

    [Test]
    public async Task ShouldLoadWorkOrderDeckWithAssigneeFilterOnInitialLoad()
    {
        await using var ctx = new BunitContext();

        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Assignee", "somename");
        navigationManager.NavigateTo(uri);

        var component = ctx.Render<WorkOrderSearch>();

        var cards = component.FindAll(".deck-card");
        cards.Count.ShouldBe(2);
    }

    [Test]
    public async Task ShouldLoadWorkOrderDeckWithStatusFilterOnInitialLoad()
    {
        await using var ctx = new BunitContext();

        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Status", WorkOrderStatus.Assigned.Key);
        navigationManager.NavigateTo(uri);

        var component = ctx.Render<WorkOrderSearch>();

        var cards = component.FindAll(".deck-card");
        cards.Count.ShouldBe(2);
    }

    [Test]
    public async Task AfterInitialLoadSelectingAllThreeOptionsShouldLoadWorkOrders()
    {
        await using var ctx = new BunitContext();

        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        var creatorSelect = component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = component.Find($"#{WorkOrderSearch.Elements.StatusSelect}");

        await creatorSelect.ChangeAsync(new() { Value = "jpalermo" });
        await assigneeSelect.ChangeAsync(new() { Value = "hsimpson" });
        await statusSelect.ChangeAsync(new() { Value = WorkOrderStatus.InProgress.Key });

        var searchButton = component.Find($"#{WorkOrderSearch.Elements.SearchButton}");
        await searchButton.ClickAsync(new());

        var deck = component.Find($"[data-testid='{WorkOrderSearch.Elements.ResultsDeck}']");
        deck.ShouldNotBeNull();

        var cards = component.FindAll(".deck-card");
        cards.Count.ShouldBe(2);
    }

    [Test]
    public async Task ShouldRenderB1CanvasWithoutSpreadsheetGrid()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        component.Find($"[data-testid='{WorkOrderSearch.Elements.B1DeckPage}']").ShouldNotBeNull();
        component.FindAll("table").Count.ShouldBe(0);
        component.FindAll(".filters-grid").Count.ShouldBe(0);
        component.Find(".filters-chip-strip").ShouldNotBeNull();
        component.Find($"[data-testid='{WorkOrderSearch.Elements.WorkOrderLink}WO-001']")
            .GetAttribute("href")
            .ShouldBe("/workorder/manage/WO-001?mode=Edit");
    }
}
