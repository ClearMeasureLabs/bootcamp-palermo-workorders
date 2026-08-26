using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSearchTests : AcceptanceTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await LoginAsCurrentUser();
    }

    [Test, Retry(2)]
    public async Task Should_PreserveMixedCaseNames_InWorkOrderSearchDropdowns()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");

        await Expect(creatorSelect.Locator("option").Filter(new() { HasText = "Timothy Lovejoy" })).ToHaveCountAsync(1);
        await Expect(assigneeSelect.Locator("option").Filter(new() { HasText = "Timothy Lovejoy" })).ToHaveCountAsync(1);

        var creatorTexts = await creatorSelect.Locator("option").AllInnerTextsAsync();
        var assigneeTexts = await assigneeSelect.Locator("option").AllInnerTextsAsync();

        creatorTexts.ShouldNotContain("TIMOTHY LOVEJOY JR");
        assigneeTexts.ShouldNotContain("TIMOTHY LOVEJOY JR");
    }

    [Test, Retry(2)]
    public async Task ShouldAssociateFilterLabelsWithSelectIds()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator($"label[for='{WorkOrderSearch.Elements.CreatorSelect}']")).ToBeAttachedAsync();
        await Expect(Page.Locator($"label[for='{WorkOrderSearch.Elements.AssigneeSelect}']")).ToBeAttachedAsync();
        await Expect(Page.Locator($"label[for='{WorkOrderSearch.Elements.StatusSelect}']")).ToBeAttachedAsync();

        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}")).ToBeVisibleAsync();
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}")).ToBeVisibleAsync();
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}")).ToBeVisibleAsync();
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
        await Expect(statusOptions).ToHaveCountAsync(WorkOrderStatus.GetAllItems().Length + 1);
        await Expect(statusOptions.First).ToHaveTextAsync("All");
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkOrderRailWithAllFiltersSetToAllOnInitialLoad()
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
        var workOrderRail = Page.GetByTestId("WorkOrderRail");
        await Expect(workOrderRail).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order1.Number)).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order2.Number)).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkOrderRailWithCreatorFilterFromQueryString()
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

        var workOrderRail = Page.GetByTestId("WorkOrderRail");
        await Expect(workOrderRail).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number)).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkOrderRailWithAssigneeFilterFromQueryString()
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

        var workOrderRail = Page.GetByTestId("WorkOrderRail");
        await Expect(workOrderRail).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number)).ToContainTextAsync(assignee.GetFullName());
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkOrderRailWithStatusFilterFromQueryString()
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

        var workOrderRail = Page.GetByTestId("WorkOrderRail");
        await Expect(workOrderRail).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number)).ToBeVisibleAsync();
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
        var workOrderRail = Page.GetByTestId("WorkOrderRail");
        await Expect(workOrderRail).ToBeVisibleAsync();
        await Expect(workOrderRail.Locator("[data-rail-card]")).ToHaveCountAsync(1);
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

        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + workOrder.Number);
        await Expect(workOrderLink).ToBeVisibleAsync();

        await workOrderLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(2, "WorkOrderDetailsPage");

        await Expect(Page).ToHaveURLAsync(new Regex($"/workorder/manage/{Regex.Escape(workOrder.Number)}"));
    }

    [Test, Retry(2)]
    public async Task ShouldChangeCenteredCardWhenNextArrowClicked()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderRail = Page.GetByTestId("WorkOrderRail");
        var initiallySelectedCard = workOrderRail.Locator("[data-rail-card][data-selected='true']");
        var initialTestId = await initiallySelectedCard.GetAttributeAsync("data-testid");

        await Page.GetByTestId("RailNext").ClickAsync();

        var newlySelectedCard = workOrderRail.Locator("[data-rail-card][data-selected='true']");
        await Expect(newlySelectedCard).ToHaveCountAsync(1);
        var newTestId = await newlySelectedCard.GetAttributeAsync("data-testid");
        newTestId.ShouldNotBe(initialTestId);
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

        var workOrderRail = Page.GetByTestId("WorkOrderRail");
        await Expect(workOrderRail).ToBeVisibleAsync();
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
        await Expect(creatorSelect).ToHaveValueAsync(CurrentUser.UserName, new() { Timeout = 30_000 });

        await Click(nameof(NavMenu.Elements.WorkOrdersAssignedToMe));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(assigneeSelect).ToHaveValueAsync(CurrentUser.UserName, new() { Timeout = 30_000 });

        await Click(nameof(NavMenu.Elements.AllWorkOrdersInProgress));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(statusSelect).ToHaveValueAsync(order1.Status.Key, new() { Timeout = 30_000 });
    }
}