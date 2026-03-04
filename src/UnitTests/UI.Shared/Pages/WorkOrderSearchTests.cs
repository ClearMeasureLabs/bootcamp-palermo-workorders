using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

public class WorkOrderSearchTests
{
    [Test]
    public void ShouldLoadDropDownsInitiallyOnLoad()
    {
        using var ctx = new TestContext();

        // Arrange
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        // Act
        var component = ctx.RenderComponent<WorkOrderSearch>();

        // Assert
        var creatorSelect = component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = component.Find($"#{WorkOrderSearch.Elements.StatusSelect}");

        creatorSelect.ShouldNotBeNull();
        assigneeSelect.ShouldNotBeNull();
        statusSelect.ShouldNotBeNull();

        // Verify user options are loaded (3 employees + "All" option = 4 options)
        var creatorOptions = creatorSelect.QuerySelectorAll("option");
        creatorOptions.Length.ShouldBe(4);
        creatorOptions[0].TextContent.ShouldBe("All");

        var assigneeOptions = assigneeSelect.QuerySelectorAll("option");
        assigneeOptions.Length.ShouldBe(4);
        assigneeOptions[0].TextContent.ShouldBe("All");

        // Verify status options are loaded (4 statuses + "All" option = 5 options)
        var statusOptions = statusSelect.QuerySelectorAll("option");
        statusOptions.Length.ShouldBe(6);
        statusOptions[0].TextContent.ShouldBe("All");
    }

    [Test]
    public void ShouldLoadWorkOrderTableWithAllFiltersSetToAllOnInitialLoad()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        // Act
        var component = ctx.RenderComponent<WorkOrderSearch>();

        // Assert
        var workOrderTable = component.Find(".grid-data");
        workOrderTable.ShouldNotBeNull();

        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public void ShouldLoadWorkOrderTableWithCreatorFilterOnInitialLoad()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Creator", "somename");
        navigationManager.NavigateTo(uri);

        // Act
        var component = ctx.RenderComponent<WorkOrderSearch>();

        // Assert
        var workOrderTable = component.Find(".grid-data");
        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public void ShouldLoadWorkOrderTableWithAssigneeFilterOnInitialLoad()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Assignee", "somename");
        navigationManager.NavigateTo(uri);

        // Act
        var component = ctx.RenderComponent<WorkOrderSearch>();

        // Assert
        var workOrderTable = component.Find(".grid-data");
        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public void ShouldLoadWorkOrderTableWithStatusFilterOnInitialLoad()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Status", WorkOrderStatus.Assigned.Key);
        navigationManager.NavigateTo(uri);

        // Act
        var component = ctx.RenderComponent<WorkOrderSearch>();

        // Assert
        var workOrderTable = component.Find(".grid-data");
        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public void AfterInitialLoadSelectingAllThreeOptionsShouldLoadWorkOrders()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.RenderComponent<WorkOrderSearch>();

        // Act
        var creatorSelect = component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = component.Find($"#{WorkOrderSearch.Elements.StatusSelect}");

        creatorSelect.Change("jpalermo");
        assigneeSelect.Change("hsimpson");
        statusSelect.Change(WorkOrderStatus.InProgress.Key);

        var searchButton = component.Find($"#{WorkOrderSearch.Elements.SearchButton}");
        searchButton.Click();

        // Assert
        var workOrderTable = component.Find(".grid-data");
        workOrderTable.ShouldNotBeNull();

        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public void WorkOrderSearch_ShouldRenderUrgencyBadge_ForEachPriority()
    {
        using var ctx = new TestContext();

        var workOrders = new[]
        {
            new WorkOrder { Number = "WO-L", Title = "Low", Status = WorkOrderStatus.Draft, Priority = WorkOrderPriority.Low },
            new WorkOrder { Number = "WO-M", Title = "Medium", Status = WorkOrderStatus.Draft, Priority = WorkOrderPriority.Medium },
            new WorkOrder { Number = "WO-H", Title = "High", Status = WorkOrderStatus.Draft, Priority = WorkOrderPriority.High },
            new WorkOrder { Number = "WO-C", Title = "Critical", Status = WorkOrderStatus.Draft, Priority = WorkOrderPriority.Critical }
        };

        ctx.Services.AddSingleton<IBus>(new StubBus(workOrders));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.RenderComponent<WorkOrderSearch>();

        component.Find("[data-testid='urgency-badge-WO-L']").ClassList.ShouldContain("urgency-low");
        component.Find("[data-testid='urgency-badge-WO-M']").ClassList.ShouldContain("urgency-medium");
        component.Find("[data-testid='urgency-badge-WO-H']").ClassList.ShouldContain("urgency-high");
        component.Find("[data-testid='urgency-badge-WO-C']").ClassList.ShouldContain("urgency-critical");
    }

    [Test]
    public void WorkOrderSearch_ShouldApplyOverdueClass_WhenPastDueDate()
    {
        using var ctx = new TestContext();

        var workOrders = new[]
        {
            new WorkOrder
            {
                Number = "WO-001",
                Title = "Overdue order",
                Status = WorkOrderStatus.Draft,
                Priority = WorkOrderPriority.High,
                DueDate = DateTime.Today.AddDays(-1)
            }
        };

        ctx.Services.AddSingleton<IBus>(new StubBus(workOrders));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.RenderComponent<WorkOrderSearch>();

        var row = component.Find("tbody tr");
        row.ClassList.ShouldContain("overdue-row");
    }

    [Test]
    public void WorkOrderSearch_ShouldNotApplyOverdueClass_WhenComplete()
    {
        using var ctx = new TestContext();

        var workOrders = new[]
        {
            new WorkOrder
            {
                Number = "WO-001",
                Title = "Completed order",
                Status = WorkOrderStatus.Complete,
                Priority = WorkOrderPriority.High,
                DueDate = DateTime.Today.AddDays(-1)
            }
        };

        ctx.Services.AddSingleton<IBus>(new StubBus(workOrders));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.RenderComponent<WorkOrderSearch>();

        var row = component.Find("tbody tr");
        row.ClassList.ShouldNotContain("overdue-row");
    }

    [Test]
    public void WorkOrderSearch_ShouldRenderUrgencyLegend()
    {
        using var ctx = new TestContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.RenderComponent<WorkOrderSearch>();

        var legend = component.Find("[data-testid='urgency-legend']");
        legend.ShouldNotBeNull();
        legend.QuerySelector(".urgency-low").ShouldNotBeNull();
        legend.QuerySelector(".urgency-medium").ShouldNotBeNull();
        legend.QuerySelector(".urgency-high").ShouldNotBeNull();
        legend.QuerySelector(".urgency-critical").ShouldNotBeNull();
    }}
