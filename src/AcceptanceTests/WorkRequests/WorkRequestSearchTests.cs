using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkRequests;

public class WorkRequestSearchTests : AcceptanceTestBase
{
    [SetUp]
    public async Task Setup()
    {
        var username = CurrentUser.UserName;
        await LoginAsCurrentUser();
    }

    [Test, Retry(2)]
    public async Task Should_PreserveMixedCaseNames_InWorkRequestSearchDropdowns()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var creatorSelect = Page.Locator($"#{WorkRequestSearch.Elements.CreatorSelect}");
        var assigneeSelect = Page.Locator($"#{WorkRequestSearch.Elements.AssigneeSelect}");

        await Expect(creatorSelect.Locator("option").Filter(new() { HasText = "Timothy Lovejoy" })).ToHaveCountAsync(1);
        await Expect(assigneeSelect.Locator("option").Filter(new() { HasText = "Timothy Lovejoy" })).ToHaveCountAsync(1);

        var creatorTexts = await creatorSelect.Locator("option").AllInnerTextsAsync();
        var assigneeTexts = await assigneeSelect.Locator("option").AllInnerTextsAsync();

        creatorTexts.ShouldNotContain("TIMOTHY LOVEJOY JR");
        assigneeTexts.ShouldNotContain("TIMOTHY LOVEJOY JR");
    }

    [Test, Retry(2)]
    public async Task ShouldLoadDropDownsInitiallyOnLoad()
    {
        // Act
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "PageLoaded");

        // Assert
        var creatorSelect = Page.Locator($"#{WorkRequestSearch.Elements.CreatorSelect}");
        var assigneeSelect = Page.Locator($"#{WorkRequestSearch.Elements.AssigneeSelect}");
        var statusSelect = Page.Locator($"#{WorkRequestSearch.Elements.StatusSelect}");

        await Expect(creatorSelect).ToBeVisibleAsync();
        await Expect(assigneeSelect).ToBeVisibleAsync();
        await Expect(statusSelect).ToBeVisibleAsync();

        // Employee count varies due to parallel test execution creating users dynamically.
        // Assert minimum count (base data has ~18 employees) plus "All" option.
        const int minimumBaseEmployees = 18;
        var creatorOptions = creatorSelect.Locator("option");
        await Expect(creatorOptions.First).ToHaveTextAsync("All");
        // Wait for employee data to finish loading via auto-retrying assertion
        await Expect(creatorOptions.Filter(new(){ HasText = "Timothy Lovejoy"})).ToHaveCountAsync(1);
        var creatorOptionCount = await creatorOptions.CountAsync();
        creatorOptionCount.ShouldBeGreaterThanOrEqualTo(minimumBaseEmployees + 1);

        var assigneeOptions = assigneeSelect.Locator("option");
        await Expect(assigneeOptions.First).ToHaveTextAsync("All");
        // Wait for employee data to finish loading via auto-retrying assertion
        await Expect(assigneeOptions.Filter(new(){ HasText = "Timothy Lovejoy"})).ToHaveCountAsync(1);
        var assigneeOptionCount = await assigneeOptions.CountAsync();
        assigneeOptionCount.ShouldBeGreaterThanOrEqualTo(minimumBaseEmployees + 1);

        // Verify status options are loaded (5 statuses + "All" option = 6 options)
        var statusOptions = statusSelect.Locator("option");
        await Expect(statusOptions).ToHaveCountAsync(WorkRequestStatus.GetAllItems().Length + 1);
        await Expect(statusOptions.First).ToHaveTextAsync("All");
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkRequestTableWithAllFiltersSetToAllOnInitialLoad()
    {
        // Arrange
        var creator = Faker<Employee>();
        var assignee = Faker<Employee>();
        var order1 = Faker<WorkRequest>();
        var order2 = Faker<WorkRequest>();
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
        var workRequestTable = Page.Locator(".grid-data");
        await Expect(workRequestTable).ToBeVisibleAsync();

        var workRequestRows = workRequestTable.Locator("tbody tr");
        var rowCount = await workRequestRows.CountAsync();
        await Expect(workRequestRows).ToHaveCountAsync(rowCount);
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkRequestTableWithCreatorFilterFromQueryString()
    {
        // Arrange
        var creator = CurrentUser;
        var order = Faker<WorkRequest>();
        order.Creator = creator;
        order.Title = $"[{TestTag}] search test";
        await using var context = TestHost.NewDbContext();
        context.Attach(creator);
        context.Add(order);
        await context.SaveChangesAsync();

        // Act
        await Click(nameof(NavMenu.Elements.MyWorkRequests));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "CreatorFiltered");

        // Assert
        var creatorSelect = Page.Locator($"#{WorkRequestSearch.Elements.CreatorSelect}");
        await Expect(creatorSelect).ToHaveValueAsync(creator.UserName);

        var workRequestTable = Page.Locator(".grid-data");
        await Expect(workRequestTable).ToBeVisibleAsync();

        var workRequestRows = workRequestTable.Locator("tbody tr");
        var rowCount = await workRequestRows.CountAsync();
        rowCount.ShouldBeGreaterThanOrEqualTo(1);
        await Expect(workRequestRows.First.Locator("td:nth-child(2)")).ToContainTextAsync(creator.GetFullName());
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkRequestTableWithAssigneeFilterFromQueryString()
    {
        // Arrange
        var creator = Faker<Employee>();
        var assignee = CurrentUser;
        var order = Faker<WorkRequest>();
        order.Creator = creator;
        order.Assignee = assignee;
        order.Title = $"[{TestTag}] assignee test";

        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        context.Attach(assignee);
        context.Add(order);
        await context.SaveChangesAsync();

        // Act
        await Click(nameof(NavMenu.Elements.WorkRequestsAssignedToMe));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "AssigneeFiltered");

        // Assert
        var assigneeSelect = Page.Locator($"#{WorkRequestSearch.Elements.AssigneeSelect}");
        await Expect(assigneeSelect).ToHaveValueAsync(assignee.UserName);

        var workRequestTable = Page.Locator(".grid-data");
        await Expect(workRequestTable).ToBeVisibleAsync();

        var workRequestRows = workRequestTable.Locator("tbody tr");
        var rowCount = await workRequestRows.CountAsync();
        rowCount.ShouldBeGreaterThanOrEqualTo(1);
        await Expect(workRequestRows.First.Locator("td:nth-child(3)")).ToContainTextAsync(assignee.GetFullName());
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkRequestTableWithStatusFilterFromQueryString()
    {
        // Arrange
        var creator = Faker<Employee>();
        var status = WorkRequestStatus.Assigned;
        var order = Faker<WorkRequest>();
        order.Creator = creator;
        order.Status = status;

        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        context.Add(order);
        await context.SaveChangesAsync();

        // Act
        await Click(nameof(NavMenu.Elements.AllAssignedWorkRequests));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "StatusFiltered");

        // Assert
        var statusSelect = Page.Locator($"#{WorkRequestSearch.Elements.StatusSelect}");
        await Expect(statusSelect).ToHaveValueAsync(status.Key);

        var workRequestTable = Page.Locator(".grid-data");
        await Expect(workRequestTable).ToBeVisibleAsync();

        var workRequestRows = workRequestTable.Locator("tbody tr");
        await Expect(workRequestRows).ToHaveCountAsync(await workRequestRows.CountAsync());
        await Expect(workRequestRows.First.Locator("td:nth-child(4)")).ToContainTextAsync(status.FriendlyName);
    }

    [Test, Retry(2)]
    public async Task ShouldSearchWithAllThreeFiltersSelected()
    {
        // Arrange
        var creator = Faker<Employee>();
        var assignee = Faker<Employee>();
        var status = Faker<WorkRequestStatus>();
        var order = Faker<WorkRequest>();
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

        var creatorSelect = Page.Locator($"#{WorkRequestSearch.Elements.CreatorSelect}");
        var assigneeSelect = Page.Locator($"#{WorkRequestSearch.Elements.AssigneeSelect}");
        var statusSelect = Page.Locator($"#{WorkRequestSearch.Elements.StatusSelect}");
        var searchButton = Page.Locator($"#{WorkRequestSearch.Elements.SearchButton}");

        await creatorSelect.SelectOptionAsync(creator.UserName);
        await assigneeSelect.SelectOptionAsync(assignee.UserName);
        await statusSelect.SelectOptionAsync(status.Key);
        await TakeScreenshotAsync(2, "FiltersSet");

        await searchButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(3, "SearchCompleted");

        // Assert
        var workRequestTable = Page.Locator(".grid-data");
        await Expect(workRequestTable).ToBeVisibleAsync();

        // Wait for the table body to update with filtered results
        await workRequestTable.Locator("tbody").WaitForAsync();
        
        var workRequestRows = workRequestTable.Locator("tbody tr");
        await Expect(workRequestRows).ToHaveCountAsync(1);
    }

    [Test, Retry(2)]
    public async Task ShouldNavigateToWorkRequestDetailsWhenClickingWorkRequestNumber()
    {
        // Arrange
        var creator = Faker<Employee>();
        var workRequest = Faker<WorkRequest>();
        workRequest.Creator = creator;

        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        context.Add(workRequest);
        await context.SaveChangesAsync();

        // Act
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "SearchPageLoaded");

        var workRequestTable = Page.Locator(".grid-data");
        await Expect(workRequestTable).ToBeVisibleAsync();

        var firstWorkRequestLink = workRequestTable.Locator("tbody tr").First.Locator("td").First.Locator("a");
        var workRequestNumber = await firstWorkRequestLink.TextContentAsync();

        if (!string.IsNullOrEmpty(workRequestNumber))
        {
            await firstWorkRequestLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await TakeScreenshotAsync(2, "WorkRequestDetailsPage");

            // Assert
            await Expect(Page).ToHaveURLAsync(new Regex($"/workrequest/manage/{Regex.Escape(workRequestNumber)}"));
        }
    }

    [Test, Retry(2)]
    public async Task ShouldClearFiltersWhenSelectingAllOption()
    {
        // Arrange
        var creator = Faker<Employee>();
        var order = Faker<WorkRequest>();
        order.Creator = creator;

        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        context.Add(order);
        await context.SaveChangesAsync();

        // Act
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var creatorSelect = Page.Locator($"#{WorkRequestSearch.Elements.CreatorSelect}");
        var searchButton = Page.Locator($"#{WorkRequestSearch.Elements.SearchButton}");

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

        var workRequestTable = Page.Locator(".grid-data");
        await Expect(workRequestTable).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldMaintainSelectedFiltersAfterSearch()
    {
        // Arrange
        var creator = Faker<Employee>();
        var assignee = Faker<Employee>();
        var status = Faker<WorkRequestStatus>();
        var order = Faker<WorkRequest>();
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

        var creatorSelect = Page.Locator($"#{WorkRequestSearch.Elements.CreatorSelect}");
        var assigneeSelect = Page.Locator($"#{WorkRequestSearch.Elements.AssigneeSelect}");
        var statusSelect = Page.Locator($"#{WorkRequestSearch.Elements.StatusSelect}");
        var searchButton = Page.Locator($"#{WorkRequestSearch.Elements.SearchButton}");

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
        var order1 = Faker<WorkRequest>();
        order1.Status = WorkRequestStatus.InProgress;
        var order2 = Faker<WorkRequest>();
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
        var creatorSelect = Page.Locator($"#{WorkRequestSearch.Elements.CreatorSelect}");
        var assigneeSelect = Page.Locator($"#{WorkRequestSearch.Elements.AssigneeSelect}");
        var statusSelect = Page.Locator($"#{WorkRequestSearch.Elements.StatusSelect}");

        await Expect(creatorSelect).ToHaveValueAsync("");
        await Expect(assigneeSelect).ToHaveValueAsync("");
        await Expect(statusSelect).ToHaveValueAsync("");

        await Click(nameof(NavMenu.Elements.MyWorkRequests));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(creatorSelect).ToHaveValueAsync(CurrentUser.UserName, new() { Timeout = 30_000 });

        await Click(nameof(NavMenu.Elements.WorkRequestsAssignedToMe));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(assigneeSelect).ToHaveValueAsync(CurrentUser.UserName, new() { Timeout = 30_000 });

        await Click(nameof(NavMenu.Elements.AllWorkRequestsInProgress));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(statusSelect).ToHaveValueAsync(order1.Status.Key, new() { Timeout = 30_000 });
    }
}