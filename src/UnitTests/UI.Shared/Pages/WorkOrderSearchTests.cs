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

        // Arrange
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        // Act
        var component = ctx.Render<WorkOrderSearch>();

        // Assert
        var creatorSelect = component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = component.Find($"#{WorkOrderSearch.Elements.StatusSelect}");

        creatorSelect.ShouldNotBeNull();
        assigneeSelect.ShouldNotBeNull();
        statusSelect.ShouldNotBeNull();

        // Verify user options are loaded (5 employees + "All" option = 6 options)
        var creatorOptions = creatorSelect.QuerySelectorAll("option");
        creatorOptions.Length.ShouldBe(6);
        creatorOptions[0].TextContent.ShouldBe("All");

        var assigneeOptions = assigneeSelect.QuerySelectorAll("option");
        assigneeOptions.Length.ShouldBe(6);
        assigneeOptions[0].TextContent.ShouldBe("All");

        // Verify status options are loaded (4 statuses + "All" option = 5 options)
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

        // Arrange
        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        // Act
        var component = ctx.Render<WorkOrderSearch>();

        // Assert
        var workOrderDeck = component.Find(".work-order-deck");
        workOrderDeck.ShouldNotBeNull();
        component.FindAll("table.grid-data").ShouldBeEmpty();

        var workOrderCards = workOrderDeck.QuerySelectorAll(".work-order-card");
        workOrderCards.Length.ShouldBe(2);
    }

    [Test]
    public async Task ShouldShowCountBadgeOnSaturdaySeriesDeck()
    {
        await using var ctx = new BunitContext();
        var assignee = new Employee("gwillie", "Willie", "MacDougal", "willie@example.com");
        var firstSaturday = new DateOnly(2026, 8, 29);
        var workOrders = Enumerable.Range(1, 10)
            .Select(index => new WorkOrder
            {
                Number = $"WO-{index:000}",
                Title = "Saturday mow",
                Status = WorkOrderStatus.Assigned,
                Assignee = assignee,
                DueDate = firstSaturday.AddDays((index - 1) * 7)
            })
            .ToArray();

        ctx.Services.AddSingleton<IBus>(new StubBus(workOrders));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        component.FindAll(".work-order-card").Count.ShouldBe(10);
        component.Find(".deck-count-badge").TextContent.ShouldBe("10");
        component.Find(".series-prompt").TextContent.ShouldContain("every Saturday");
    }

    [Test]
    public async Task ShouldLoadWorkOrderDeckWithCreatorFilterOnInitialLoad()
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
        var workOrderDeck = component.Find(".work-order-deck");
        var workOrderCards = workOrderDeck.QuerySelectorAll(".work-order-card");
        workOrderCards.Length.ShouldBe(2);
    }

    [Test]
    public async Task ShouldLoadWorkOrderDeckWithAssigneeFilterOnInitialLoad()
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
        var workOrderDeck = component.Find(".work-order-deck");
        var workOrderCards = workOrderDeck.QuerySelectorAll(".work-order-card");
        workOrderCards.Length.ShouldBe(2);
    }

    [Test]
    public async Task ShouldLoadWorkOrderDeckWithStatusFilterOnInitialLoad()
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
        var workOrderDeck = component.Find(".work-order-deck");
        var workOrderCards = workOrderDeck.QuerySelectorAll(".work-order-card");
        workOrderCards.Length.ShouldBe(2);
    }

    [Test]
    public async Task AfterInitialLoadSelectingAllThreeOptionsShouldLoadWorkOrders()
    {
        await using var ctx = new BunitContext();

        // Arrange
        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        // Act
        var creatorSelect = component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = component.Find($"#{WorkOrderSearch.Elements.StatusSelect}");

        await creatorSelect.ChangeAsync(new() { Value = "jpalermo" });
        await assigneeSelect.ChangeAsync(new() { Value = "hsimpson" });
        await statusSelect.ChangeAsync(new() { Value = WorkOrderStatus.InProgress.Key });

        var searchButton = component.Find($"#{WorkOrderSearch.Elements.SearchButton}");
        await searchButton.ClickAsync(new());

        // Assert
        var workOrderDeck = component.Find(".work-order-deck");
        workOrderDeck.ShouldNotBeNull();

        var workOrderCards = workOrderDeck.QuerySelectorAll(".work-order-card");
        workOrderCards.Length.ShouldBe(2);
    }
}