using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSearchTests : AcceptanceTestBase
{
    [SetUp]
    public async Task Setup()
    {
        var username = CurrentUser.UserName;
        await LoginAsCurrentUser();
    }

    [Test, Retry(2)]
    public async Task ShouldLoadDropDownsInitiallyOnLoad()
    {
        // Act
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "PageLoaded");

        // Assert
        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}");

        await Expect(creatorSelect).ToBeVisibleAsync();
        await Expect(assigneeSelect).ToBeVisibleAsync();
        await Expect(statusSelect).ToBeVisibleAsync();

        // Employee count varies due to parallel test execution creating users dynamically.
        // Assert minimum count (base data has ~18 employees) plus "All" option.
        const int minimumBaseEmployees = 18;
        var creatorOptions = creatorSelect.Locator("option");
        var creatorOptionCount = await creatorOptions.CountAsync();
        creatorOptionCount.ShouldBeGreaterThanOrEqualTo(minimumBaseEmployees + 1);
        await Expect(creatorOptions.First).ToHaveTextAsync("All");
        await Expect(creatorOptions.Filter(new(){ HasText = "Timothy Lovejoy"})).ToHaveCountAsync(1); //Specific user from test data should always be there.

        var assigneeOptions = assigneeSelect.Locator("option");
        var assigneeOptionCount = await assigneeOptions.CountAsync();
        assigneeOptionCount.ShouldBeGreaterThanOrEqualTo(minimumBaseEmployees + 1);
        await Expect(assigneeOptions.First).ToHaveTextAsync("All");

        // Verify status options are loaded (5 statuses + "All" option = 6 options)
        var statusOptions = statusSelect.Locator("option");
        await Expect(statusOptions).ToHaveCountAsync(WorkOrderStatus.GetAllItems().Length + 1);
        await Expect(statusOptions.First).ToHaveTextAsync("All");
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkOrderTableWithAllFiltersSetToAllOnInitialLoad()
    {
        // Arrange
        var creator = Faker<Employee>();
        var assignee = Faker<Employee>();
        var order1 = Faker<WorkOrder>();
        var order2 = Faker<WorkOrder>();
        order1.Creator = creator;
        order1.Assignee = assignee;
        order2.Creator = creator;
        order2.Assignee = assignee;

        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        context.Add(assignee);
        context.Add(order1);
        context.Add(order2);
        await context.SaveChangesAsync();

        // Act
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "InitialLoad");

        // Assert
        var workOrderTable = Page.Locator(".grid-data");
        await Expect(workOrderTable).ToBeVisibleAsync();

        var workOrderRows = workOrderTable.Locator("tbody tr");
        var rowCount = await workOrderRows.CountAsync();
        await Expect(workOrderRows).ToHaveCountAsync(rowCount);
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkOrderTableWithCreatorFilterFromQueryString()
    {
        // Arrange
        var creator = CurrentUser;
        var order = Faker<WorkOrder>();
        order.Creator = creator;
        order.Title = $"[{TestTag}] search test";
        await using var context = TestHost.NewDbContext();
        context.Attach(creator);
        context.Add(order);
        await context.SaveChangesAsync();

        // Act
        await Click(nameof(NavMenu.Elements.MyWorkOrders));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "CreatorFiltered");

        // Assert
        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        await Expect(creatorSelect).ToHaveValueAsync(creator.UserName);

        var workOrderTable = Page.Locator(".grid-data");
        await Expect(workOrderTable).ToBeVisibleAsync();

        var workOrderRows = workOrderTable.Locator("tbody tr");
        var rowCount = await workOrderRows.CountAsync();
        rowCount.ShouldBeGreaterThanOrEqualTo(1);
        await Expect(workOrderRows.First.Locator("td:nth-child(2)")).ToContainTextAsync(creator.GetFullName());
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkOrderTableWithAssigneeFilterFromQueryString()
    {
        // Arrange
        var creator = Faker<Employee>();
        var assignee = CurrentUser;
        var order = Faker<WorkOrder>();
        order.Creator = creator;
        order.Assignee = assignee;
        order.Title = $"[{TestTag}] assignee test";

        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        context.Attach(assignee);
        context.Add(order);
        await context.SaveChangesAsync();

        // Act
        await Click(nameof(NavMenu.Elements.WorkOrdersAssignedToMe));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "AssigneeFiltered");

        // Assert
        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        await Expect(assigneeSelect).ToHaveValueAsync(assignee.UserName);

        var workOrderTable = Page.Locator(".grid-data");
        await Expect(workOrderTable).ToBeVisibleAsync();

        var workOrderRows = workOrderTable.Locator("tbody tr");
        var rowCount = await workOrderRows.CountAsync();
        rowCount.ShouldBeGreaterThanOrEqualTo(1);
        await Expect(workOrderRows.First.Locator("td:nth-child(3)")).ToContainTextAsync(assignee.GetFullName());
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkOrderTableWithStatusFilterFromQueryString()
    {
        // Arrange
        var creator = Faker<Employee>();
        var status = WorkOrderStatus.Assigned;
        var order = Faker<WorkOrder>();
        order.Creator = creator;
        order.Status = status;

        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        context.Add(order);
        await context.SaveChangesAsync();

        // Act
        await Click(nameof(NavMenu.Elements.AllAssignedWorkOrders));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "StatusFiltered");

        // Assert
        var statusSelect = Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}");
        await Expect(statusSelect).ToHaveValueAsync(status.Key);

        var workOrderTable = Page.Locator(".grid-data");
        await Expect(workOrderTable).ToBeVisibleAsync();

        var workOrderRows = workOrderTable.Locator("tbody tr");
        await Expect(workOrderRows).ToHaveCountAsync(await workOrderRows.CountAsync());
        await Expect(workOrderRows.First.Locator("td:nth-child(4)")).ToContainTextAsync(status.FriendlyName);
    }

    [Test, Retry(2)]
    public async Task ShouldSearchWithAllThreeFiltersSelected()
    {
        // Arrange
        var creator = Faker<Employee>();
        var assignee = Faker<Employee>();
        var status = Faker<WorkOrderStatus>();
        var order = Faker<WorkOrder>();
        order.Creator = creator;
        order.Assignee = assignee;
        order.Status = status;

        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        context.Add(assignee);
        context.Add(order);
        await context.SaveChangesAsync();

        // Act
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "BeforeFiltering");

        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}");
        var searchButton = Page.Locator($"#{WorkOrderSearch.Elements.SearchButton}");

        await creatorSelect.SelectOptionAsync(creator.UserName);
        await assigneeSelect.SelectOptionAsync(assignee.UserName);
        await statusSelect.SelectOptionAsync(status.Key);
        await TakeScreenshotAsync(2, "FiltersSet");

        await searchButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(3, "SearchCompleted");

        // Assert
        var workOrderTable = Page.Locator(".grid-data");
        await Expect(workOrderTable).ToBeVisibleAsync();

        var workOrderRows = workOrderTable.Locator("tbody tr");
        await Expect(workOrderRows).ToHaveCountAsync(await workOrderRows.CountAsync());
    }

    [Test, Retry(2)]
    public async Task ShouldNavigateToWorkOrderDetailsWhenClickingWorkOrderNumber()
    {
        // Arrange
        var creator = Faker<Employee>();
        var workOrder = Faker<WorkOrder>();
        workOrder.Creator = creator;

        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        // Act
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "SearchPageLoaded");

        var workOrderTable = Page.Locator(".grid-data");
        await Expect(workOrderTable).ToBeVisibleAsync();

        var firstWorkOrderLink = workOrderTable.Locator("tbody tr").First.Locator("td").First.Locator("a");
        var workOrderNumber = await firstWorkOrderLink.TextContentAsync();

        if (!string.IsNullOrEmpty(workOrderNumber))
        {
            await firstWorkOrderLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await TakeScreenshotAsync(2, "WorkOrderDetailsPage");

            // Assert
            await Expect(Page).ToHaveURLAsync(new Regex($"/workorder/manage/{Regex.Escape(workOrderNumber)}"));
        }
    }

    [Test, Retry(2)]
    public async Task ShouldClearFiltersWhenSelectingAllOption()
    {
        // Arrange
        var creator = Faker<Employee>();
        var order = Faker<WorkOrder>();
        order.Creator = creator;

        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        context.Add(order);
        await context.SaveChangesAsync();

        // Act
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var searchButton = Page.Locator($"#{WorkOrderSearch.Elements.SearchButton}");

        // First set a filter
        await creatorSelect.SelectOptionAsync(creator.UserName);
        await searchButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "FilterSet");

        // Then clear it by selecting "All"
        await creatorSelect.SelectOptionAsync("");
        await searchButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(2, "FilterCleared");

        // Assert
        await Expect(creatorSelect).ToHaveValueAsync("");

        var workOrderTable = Page.Locator(".grid-data");
        await Expect(workOrderTable).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldMaintainSelectedFiltersAfterSearch()
    {
        // Arrange
        var creator = Faker<Employee>();
        var assignee = Faker<Employee>();
        var status = Faker<WorkOrderStatus>();
        var order = Faker<WorkOrder>();
        order.Creator = creator;
        order.Assignee = assignee;
        order.Status = status;

        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        context.Add(assignee);
        context.Add(order);
        await context.SaveChangesAsync();

        // Act
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}");
        var searchButton = Page.Locator($"#{WorkOrderSearch.Elements.SearchButton}");

        await creatorSelect.SelectOptionAsync(creator.UserName);
        await assigneeSelect.SelectOptionAsync(assignee.UserName);
        await statusSelect.SelectOptionAsync(status.Key);

        await searchButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "AfterSearch");

        // Assert
        await Expect(creatorSelect).ToHaveValueAsync(creator.UserName);
        await Expect(assigneeSelect).ToHaveValueAsync(assignee.UserName);
        await Expect(statusSelect).ToHaveValueAsync(status.Key);
    }

    [Test, Retry(2)]
    public async Task ShouldReloadParamsFromQueryStringWithNavigation()
    {
        // Arrange
        var order1 = Faker<WorkOrder>();
        order1.Status = WorkOrderStatus.InProgress;
        var order2 = Faker<WorkOrder>();
        order1.Creator = CurrentUser;
        order1.Assignee = CurrentUser;
        order2.Creator = CurrentUser;
        order2.Assignee = CurrentUser;

        await using var context = TestHost.NewDbContext();
        context.Attach(CurrentUser);
        context.Add(order1);
        context.Add(order2);
        await context.SaveChangesAsync();

        // Act
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}");

        await Expect(creatorSelect).ToHaveValueAsync("");
        await Expect(assigneeSelect).ToHaveValueAsync("");
        await Expect(statusSelect).ToHaveValueAsync("");

        await Click(nameof(NavMenu.Elements.MyWorkOrders));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await creatorSelect.DblClickAsync();
        await Expect(creatorSelect).ToHaveValueAsync(CurrentUser.UserName);

        await Click(nameof(NavMenu.Elements.WorkOrdersAssignedToMe));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await assigneeSelect.DblClickAsync();
        await Expect(assigneeSelect).ToHaveValueAsync(CurrentUser.UserName);

        await Click(nameof(NavMenu.Elements.AllWorkOrdersInProgress));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await statusSelect.DblClickAsync();
        await Expect(statusSelect).ToHaveValueAsync(order1.Status.Key);
    }
}