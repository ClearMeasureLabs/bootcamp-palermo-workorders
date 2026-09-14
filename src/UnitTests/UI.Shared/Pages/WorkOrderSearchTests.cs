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
    public async Task ShouldLoadWorkOrderTableWithAllFiltersSetToAllOnInitialLoad()
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
        var workOrderTable = component.Find(".grid-data");
        workOrderTable.ShouldNotBeNull();

        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public async Task ShouldLoadWorkOrderTableWithCreatorFilterOnInitialLoad()
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
        var workOrderTable = component.Find(".grid-data");
        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public async Task ShouldLoadWorkOrderTableWithAssigneeFilterOnInitialLoad()
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
        var workOrderTable = component.Find(".grid-data");
        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public async Task ShouldLoadWorkOrderTableWithStatusFilterOnInitialLoad()
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
        var workOrderTable = component.Find(".grid-data");
        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
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
        var workOrderTable = component.Find(".grid-data");
        workOrderTable.ShouldNotBeNull();

        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public async Task ClearFiltersButton_ShouldBeDisabled_WhenAllFiltersAreEmpty()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        var clearButton = component.Find($"#{WorkOrderSearch.Elements.ClearFiltersButton}");
        clearButton.HasAttribute("disabled").ShouldBeTrue();
    }

    [Test]
    public async Task ClearFiltersButton_ShouldBeEnabled_WhenAtLeastOneFilterIsSet()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        var creatorSelect = component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}");
        await creatorSelect.ChangeAsync(new() { Value = "jpalermo" });

        var clearButton = component.Find($"#{WorkOrderSearch.Elements.ClearFiltersButton}");
        clearButton.HasAttribute("disabled").ShouldBeFalse();
    }

    [Test]
    public async Task SortByStatus_Ascending_SortsResultsByStatusFriendlyName()
    {
        await using var ctx = new BunitContext();
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.InProgress },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Assigned },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft },
        };
        ctx.Services.AddSingleton<IBus>(new StubBus(rows));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByStatusButton}");
        await sortBtn.ClickAsync(new());

        var cells = component.FindAll("tbody tr td:nth-child(4)");
        cells[0].TextContent.Trim().ShouldBe(WorkOrderStatus.Assigned.FriendlyName);
        cells[1].TextContent.Trim().ShouldBe(WorkOrderStatus.Draft.FriendlyName);
        cells[2].TextContent.Trim().ShouldBe(WorkOrderStatus.InProgress.FriendlyName);
    }

    [Test]
    public async Task SortByStatus_ClickingAgain_ReversesToDescending()
    {
        await using var ctx = new BunitContext();
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.InProgress },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Assigned },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft },
        };
        ctx.Services.AddSingleton<IBus>(new StubBus(rows));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByStatusButton}");
        await sortBtn.ClickAsync(new());
        await sortBtn.ClickAsync(new());

        var cells = component.FindAll("tbody tr td:nth-child(4)");
        cells[0].TextContent.Trim().ShouldBe(WorkOrderStatus.InProgress.FriendlyName);
        cells[1].TextContent.Trim().ShouldBe(WorkOrderStatus.Draft.FriendlyName);
        cells[2].TextContent.Trim().ShouldBe(WorkOrderStatus.Assigned.FriendlyName);
    }

    [Test]
    public async Task SortByDueDate_Ascending_SortsResultsByDueDate_NullsLast()
    {
        await using var ctx = new BunitContext();
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.Draft, DueDate = null },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Draft, DueDate = new DateOnly(2025, 6, 1) },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft, DueDate = new DateOnly(2025, 3, 1) },
        };
        ctx.Services.AddSingleton<IBus>(new StubBus(rows));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByDueDateButton}");
        await sortBtn.ClickAsync(new());

        var tds = component.FindAll("tbody tr");
        tds[0].QuerySelector("td")!.TextContent.Trim().ShouldBe("WO-002");
        tds[1].QuerySelector("td")!.TextContent.Trim().ShouldBe("WO-001");
        tds[2].QuerySelector("td")!.TextContent.Trim().ShouldBe("WO-003");
    }

    [Test]
    public async Task SortByDueDate_ClickingAgain_ReversesToDescending_NullsLast()
    {
        await using var ctx = new BunitContext();
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.Draft, DueDate = null },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Draft, DueDate = new DateOnly(2025, 6, 1) },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft, DueDate = new DateOnly(2025, 3, 1) },
        };
        ctx.Services.AddSingleton<IBus>(new StubBus(rows));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByDueDateButton}");
        await sortBtn.ClickAsync(new());
        await sortBtn.ClickAsync(new());

        var tds = component.FindAll("tbody tr");
        tds[0].QuerySelector("td")!.TextContent.Trim().ShouldBe("WO-001");
        tds[1].QuerySelector("td")!.TextContent.Trim().ShouldBe("WO-002");
        tds[2].QuerySelector("td")!.TextContent.Trim().ShouldBe("WO-003");
    }

    [Test]
    public async Task SortByDifferentColumn_ResetsDirectionToAscending()
    {
        await using var ctx = new BunitContext();
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.InProgress, DueDate = new DateOnly(2025, 6, 1) },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Assigned, DueDate = new DateOnly(2025, 3, 1) },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft, DueDate = new DateOnly(2025, 9, 1) },
        };
        ctx.Services.AddSingleton<IBus>(new StubBus(rows));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        // Click Status twice → descending
        var statusBtn = component.Find($"#{WorkOrderSearch.Elements.SortByStatusButton}");
        await statusBtn.ClickAsync(new());
        await statusBtn.ClickAsync(new());

        // Now click DueDate → should reset to ascending
        var dueDateBtn = component.Find($"#{WorkOrderSearch.Elements.SortByDueDateButton}");
        await dueDateBtn.ClickAsync(new());

        var tds = component.FindAll("tbody tr");
        tds[0].QuerySelector("td")!.TextContent.Trim().ShouldBe("WO-001");
        tds[1].QuerySelector("td")!.TextContent.Trim().ShouldBe("WO-003");
        tds[2].QuerySelector("td")!.TextContent.Trim().ShouldBe("WO-002");
    }

    [Test]
    public async Task SortIndicatorGlyph_ShowsAscendingGlyph_WhenColumnFirstClicked()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByStatusButton}");
        await sortBtn.ClickAsync(new());

        sortBtn.TextContent.ShouldContain("▲");
        sortBtn.TextContent.ShouldNotContain("▼");
    }

    [Test]
    public async Task SortIndicatorGlyph_ShowsDescendingGlyph_WhenSameColumnClickedAgain()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByStatusButton}");
        await sortBtn.ClickAsync(new());
        await sortBtn.ClickAsync(new());

        sortBtn.TextContent.ShouldContain("▼");
        sortBtn.TextContent.ShouldNotContain("▲");
    }

    [Test]
    public async Task ClickingClearFiltersButton_ShouldResetAllFiltersToEmpty_AndTriggerSearch()
    {
        await using var ctx = new BunitContext();

        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<WorkOrderSearch>();

        // Set all three filters
        var creatorSelect = component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = component.Find($"#{WorkOrderSearch.Elements.StatusSelect}");

        await creatorSelect.ChangeAsync(new() { Value = "jpalermo" });
        await assigneeSelect.ChangeAsync(new() { Value = "hsimpson" });
        await statusSelect.ChangeAsync(new() { Value = WorkOrderStatus.InProgress.Key });

        var sendCountBeforeClear = stubBus.SendCallCount;

        // Click clear
        var clearButton = component.Find($"#{WorkOrderSearch.Elements.ClearFiltersButton}");
        await clearButton.ClickAsync(new());

        // Assert selects reset
        component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}").GetAttribute("value").ShouldBeNullOrEmpty();
        component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}").GetAttribute("value").ShouldBeNullOrEmpty();
        component.Find($"#{WorkOrderSearch.Elements.StatusSelect}").GetAttribute("value").ShouldBeNullOrEmpty();

        // Assert search was re-run (at least one more Send call after clearing)
        stubBus.SendCallCount.ShouldBeGreaterThan(sendCountBeforeClear);
    }
}