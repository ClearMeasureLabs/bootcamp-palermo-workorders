using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

public class WorkOrderSearchTests
{
    private const string CurrentUsername = "jpalermo";

    private static BunitContext CreateContext(IBus? bus = null, string loggedInAs = CurrentUsername,
        WorkOrderSearchState? searchState = null)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton(bus ?? new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton(searchState ?? new WorkOrderSearchState());

        var store = new StubUserSessionStore { Username = loggedInAs };
        var authProvider = new CustomAuthenticationStateProvider(store);
        authProvider.Login(loggedInAs).GetAwaiter().GetResult();
        ctx.Services.AddSingleton<AuthenticationStateProvider>(authProvider);

        return ctx;
    }

    [Test]
    public async Task ShouldLoadDropDownsInitiallyOnLoad()
    {
        await using var ctx = CreateContext();

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
        await using var ctx = CreateContext();

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
        await using var ctx = CreateContext();

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
        await using var ctx = CreateContext();

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
        await using var ctx = CreateContext();

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
        await using var ctx = CreateContext();

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
        await using var ctx = CreateContext();

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
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var clearButton = component.Find($"#{WorkOrderSearch.Elements.ClearFiltersButton}");
        clearButton.HasAttribute("disabled").ShouldBeTrue();
    }

    [Test]
    public async Task ClearFiltersButton_ShouldBeEnabled_WhenAtLeastOneFilterIsSet()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var creatorSelect = component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}");
        await creatorSelect.ChangeAsync(new() { Value = "jpalermo" });

        var clearButton = component.Find($"#{WorkOrderSearch.Elements.ClearFiltersButton}");
        clearButton.HasAttribute("disabled").ShouldBeFalse();
    }

    [Test]
    public async Task SortByStatus_Ascending_SortsResultsByStatusFriendlyName()
    {
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.InProgress },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Assigned },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft },
        };
        await using var ctx = CreateContext(new StubBus(rows));

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
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.InProgress },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Assigned },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft },
        };
        await using var ctx = CreateContext(new StubBus(rows));

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
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.Draft, DueDate = null },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Draft, DueDate = new DateOnly(2025, 6, 1) },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft, DueDate = new DateOnly(2025, 3, 1) },
        };
        await using var ctx = CreateContext(new StubBus(rows));

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
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.Draft, DueDate = null },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Draft, DueDate = new DateOnly(2025, 6, 1) },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft, DueDate = new DateOnly(2025, 3, 1) },
        };
        await using var ctx = CreateContext(new StubBus(rows));

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
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.InProgress, DueDate = new DateOnly(2025, 6, 1) },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Assigned, DueDate = new DateOnly(2025, 3, 1) },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft, DueDate = new DateOnly(2025, 9, 1) },
        };
        await using var ctx = CreateContext(new StubBus(rows));

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
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByStatusButton}");
        await sortBtn.ClickAsync(new());

        sortBtn.TextContent.ShouldContain("▲");
        sortBtn.TextContent.ShouldNotContain("▼");
    }

    [Test]
    public async Task SortIndicatorGlyph_ShowsDescendingGlyph_WhenSameColumnClickedAgain()
    {
        await using var ctx = CreateContext();

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
        var stubBus = new StubBus();
        await using var ctx = CreateContext(stubBus);

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

    [Test]
    public async Task SortByTitle_Ascending_SortsResultsByTitle()
    {
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.Draft },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Draft },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft },
        };
        await using var ctx = CreateContext(new StubBus(rows));

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByTitleButton}");
        await sortBtn.ClickAsync(new());

        var cells = component.FindAll("tbody tr td:nth-child(5)");
        cells[0].TextContent.Trim().ShouldBe("A");
        cells[1].TextContent.Trim().ShouldBe("B");
        cells[2].TextContent.Trim().ShouldBe("C");
    }

    [Test]
    public async Task SortByTitle_ClickingAgain_ReversesToDescending()
    {
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.Draft },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Draft },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft },
        };
        await using var ctx = CreateContext(new StubBus(rows));

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByTitleButton}");
        await sortBtn.ClickAsync(new());
        await sortBtn.ClickAsync(new());

        var cells = component.FindAll("tbody tr td:nth-child(5)");
        cells[0].TextContent.Trim().ShouldBe("C");
        cells[1].TextContent.Trim().ShouldBe("B");
        cells[2].TextContent.Trim().ShouldBe("A");
    }

    [Test]
    public async Task SortByRoom_Ascending_SortsResultsByRoomNumber()
    {
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.Draft, RoomNumber = "C" },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Draft, RoomNumber = "A" },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft, RoomNumber = "B" },
        };
        await using var ctx = CreateContext(new StubBus(rows));

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByRoomButton}");
        await sortBtn.ClickAsync(new());

        var cells = component.FindAll("tbody tr td:nth-child(7)");
        cells[0].TextContent.Trim().ShouldBe("A");
        cells[1].TextContent.Trim().ShouldBe("B");
        cells[2].TextContent.Trim().ShouldBe("C");
    }

    [Test]
    public async Task SortByRoom_ClickingAgain_ReversesToDescending()
    {
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.Draft, RoomNumber = "C" },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Draft, RoomNumber = "A" },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft, RoomNumber = "B" },
        };
        await using var ctx = CreateContext(new StubBus(rows));

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByRoomButton}");
        await sortBtn.ClickAsync(new());
        await sortBtn.ClickAsync(new());

        var cells = component.FindAll("tbody tr td:nth-child(7)");
        cells[0].TextContent.Trim().ShouldBe("C");
        cells[1].TextContent.Trim().ShouldBe("B");
        cells[2].TextContent.Trim().ShouldBe("A");
    }
    [Test]
    public async Task AssignedToMeCheckbox_WhenSessionStateIsTrue_RestoresCheckboxAndFiltersOnLoad()
    {
        // Pre-populate the search state so InitializeAsync restores the AssignedToMe=true branch
        var state = new WorkOrderSearchState { AssignedToMe = true };
        var stubBus = new StubBusWithAssigneeCapture();
        await using var ctx = CreateContext(stubBus, searchState: state);

        var component = ctx.Render<WorkOrderSearch>();

        // Checkbox should be checked (restored from session state)
        var checkbox = component.Find($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}");
        checkbox.HasAttribute("checked").ShouldBeTrue();

        // Assignee select should be disabled
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        assigneeSelect.HasAttribute("disabled").ShouldBeTrue();

        // The query sent to the bus should have the current user's username as assignee
        stubBus.LastAssigneeQueried.ShouldBe(CurrentUsername);
    }

    [Test]
    public async Task AssignedToMeCheckbox_ShouldBeRendered()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var checkbox = component.Find($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}");
        checkbox.ShouldNotBeNull();
        checkbox.HasAttribute("checked").ShouldBeFalse();
    }

    [Test]
    public async Task AssignedToMeCheckbox_WhenChecked_SetsAssigneeToCurrentUser()
    {
        var stubBus = new StubBusWithAssigneeCapture();
        await using var ctx = CreateContext(stubBus);

        var component = ctx.Render<WorkOrderSearch>();

        var checkbox = component.Find($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}");
        await checkbox.ChangeAsync(new() { Value = true });

        // Assignee select should be disabled
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        assigneeSelect.HasAttribute("disabled").ShouldBeTrue();

        // The query sent to the bus should have the current user's username as assignee
        stubBus.LastAssigneeQueried.ShouldBe(CurrentUsername);
    }

    [Test]
    public async Task AssignedToMeCheckbox_WhenUnchecked_ClearsAssigneeFilter()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var checkbox = component.Find($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}");
        // Check then uncheck
        await checkbox.ChangeAsync(new() { Value = true });
        await checkbox.ChangeAsync(new() { Value = false });

        // Assignee dropdown should be re-enabled
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        assigneeSelect.HasAttribute("disabled").ShouldBeFalse();

        // Assignee filter should be cleared
        assigneeSelect.GetAttribute("value").ShouldBeNullOrEmpty();
    }

    [Test]
    public async Task ClearFiltersButton_WhenClicked_ResetsAssignedToMeCheckbox()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var checkbox = component.Find($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}");
        await checkbox.ChangeAsync(new() { Value = true });

        // Clear filters
        var clearButton = component.Find($"#{WorkOrderSearch.Elements.ClearFiltersButton}");
        await clearButton.ClickAsync(new());

        // Checkbox should be unchecked
        component.Find($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}").HasAttribute("checked").ShouldBeFalse();

        // Clear button should be disabled (no active filters)
        clearButton.HasAttribute("disabled").ShouldBeTrue();
    }

    [Test]
    public async Task ShouldApply_OverdueRow_CssClass_WhenWorkOrderIsOverdue()
    {
        // An overdue work order: past due date + open status
        var overdueOrder = new WorkOrder
        {
            Number = "WO-OVR",
            Title = "Overdue",
            Status = WorkOrderStatus.InProgress,
            DueDate = new DateOnly(2000, 1, 1)
        };

        await using var ctx = CreateContext(new StubBus([overdueOrder]));

        var component = ctx.Render<WorkOrderSearch>();

        var rows = component.FindAll("tbody tr");
        rows.Count.ShouldBe(1);
        rows[0].ClassName!.ShouldContain("overdue-row");
        rows[0].GetAttribute("aria-label").ShouldBe("Overdue work order");
    }

    [Test]
    public async Task ShouldNotApply_OverdueRow_CssClass_WhenWorkOrderIsNotOverdue()
    {
        var nonOverdueOrder = new WorkOrder
        {
            Number = "WO-FUT",
            Title = "Future",
            Status = WorkOrderStatus.InProgress,
            DueDate = new DateOnly(2099, 12, 31)
        };

        await using var ctx = CreateContext(new StubBus([nonOverdueOrder]));

        var component = ctx.Render<WorkOrderSearch>();

        var rows = component.FindAll("tbody tr");
        rows.Count.ShouldBe(1);
        rows[0].ClassName!.ShouldNotContain("overdue-row");
        rows[0].GetAttribute("aria-label").ShouldBeNull();
    }

    [Test]
    public async Task ShouldReport_HasActiveFilters_True_WhenOverdueOnlyIsSet()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        // Toggle the checkbox
        var toggle = component.Find($"#{WorkOrderSearch.Elements.OverdueOnlyToggle}");
        await toggle.ChangeAsync(new() { Value = true });

        var clearButton = component.Find($"#{WorkOrderSearch.Elements.ClearFiltersButton}");
        clearButton.HasAttribute("disabled").ShouldBeFalse();
    }

    [Test]
    public async Task ShouldReset_OverdueOnly_OnClearFilters()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();
        var instance = component.Instance;

        // Set the toggle
        var toggle = component.Find($"#{WorkOrderSearch.Elements.OverdueOnlyToggle}");
        await toggle.ChangeAsync(new() { Value = true });
        instance.Model.Filters.OverdueOnly.ShouldBeTrue();

        // Clear filters
        var clearButton = component.Find($"#{WorkOrderSearch.Elements.ClearFiltersButton}");
        await clearButton.ClickAsync(new());

        instance.Model.Filters.OverdueOnly.ShouldBeFalse();
    }

    [Test]
    public void ShouldRemotableRequest_RoundTrip_OverdueOnly()
    {
        var query = new ClearMeasure.Bootcamp.Core.Queries.WorkOrderSpecificationQuery();
        query.MatchOverdueOnly(true);

        var json = System.Text.Json.JsonSerializer.Serialize(query);
        var rehydrated = System.Text.Json.JsonSerializer.Deserialize<ClearMeasure.Bootcamp.Core.Queries.WorkOrderSpecificationQuery>(json);

        rehydrated.ShouldNotBeNull();
        rehydrated.OverdueOnly.ShouldBeTrue();
    }
}
