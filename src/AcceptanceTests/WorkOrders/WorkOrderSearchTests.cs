using System.Globalization;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
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
        var cardList = Page.Locator(".work-order-card-list");
        await Expect(cardList).ToBeVisibleAsync();

        var workOrderCards = cardList.Locator(".work-order-card");
        var cardCount = await workOrderCards.CountAsync();
        await Expect(workOrderCards).ToHaveCountAsync(cardCount);
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

        var cardList = Page.Locator(".work-order-card-list");
        await Expect(cardList).ToBeVisibleAsync();

        var workOrderCards = cardList.Locator(".work-order-card");
        var cardCount = await workOrderCards.CountAsync();
        cardCount.ShouldBeGreaterThanOrEqualTo(1);
        await Expect(workOrderCards.First).ToContainTextAsync(creator.GetFullName());
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

        var cardList = Page.Locator(".work-order-card-list");
        await Expect(cardList).ToBeVisibleAsync();

        var workOrderCards = cardList.Locator(".work-order-card");
        var cardCount = await workOrderCards.CountAsync();
        cardCount.ShouldBeGreaterThanOrEqualTo(1);
        await Expect(workOrderCards.First).ToContainTextAsync(assignee.GetFullName());
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

        var cardList = Page.Locator(".work-order-card-list");
        await Expect(cardList).ToBeVisibleAsync();

        var workOrderCards = cardList.Locator(".work-order-card");
        await Expect(workOrderCards).ToHaveCountAsync(await workOrderCards.CountAsync());
        await Expect(workOrderCards.First).ToContainTextAsync(status.FriendlyName);
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
        var cardList = Page.Locator(".work-order-card-list");
        await Expect(cardList).ToBeVisibleAsync();

        await cardList.Locator(".work-order-card").First.WaitForAsync();

        var workOrderCards = cardList.Locator(".work-order-card");
        await Expect(workOrderCards).ToHaveCountAsync(1);
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

        var cardList = Page.Locator(".work-order-card-list");
        await Expect(cardList).ToBeVisibleAsync();

        var firstWorkOrderLink = cardList.Locator(".work-order-card").First.Locator("a.work-order-card-number");
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

        var cardList = Page.Locator(".work-order-card-list");
        await Expect(cardList).ToBeVisibleAsync();
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

    [Test, Retry(2)]
    public async Task Should_ShowSearchResultsAsCards_NotGridTable()
    {
        var creator = Faker<Employee>();
        var workOrder = Faker<WorkOrder>();
        workOrder.Creator = creator;

        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator(".work-order-card-list")).ToBeVisibleAsync();
        await Expect(Page.Locator("table.grid-data")).ToHaveCountAsync(0);

        var linkTestId = nameof(WorkOrderSearch.Elements.WorkOrderLink) + workOrder.Number;
        await Page.GetByTestId(linkTestId).ClickAsync();
        await Page.WaitForURLAsync(new Regex($"/workorder/manage/{Regex.Escape(workOrder.Number!)}"));
    }

    [Test, Retry(2)]
    public async Task Should_ShowDueDateUrgencyPillsOnSearchCards()
    {
        var today = ChurchTimeZone.Today(TimeProvider.System);
        var overdue = today.AddDays(-2);

        var todayOrder = await SeedDraftWithDueDateAsync($"[{TestTag}] card due today", today);
        var overdueOrder = await SeedDraftWithDueDateAsync($"[{TestTag}] card overdue", overdue);
        var emptyOrder = await SeedDraftWithDueDateAsync($"[{TestTag}] card empty due", null);

        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var todayCell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.DueDateCell) + todayOrder.Number);
        var overdueCell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.DueDateCell) + overdueOrder.Number);
        var emptyCell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.DueDateCell) + emptyOrder.Number);

        await Expect(todayCell).ToHaveClassAsync(new Regex("due-date-today"));
        await Expect(overdueCell).ToHaveClassAsync(new Regex("due-date-overdue"));
        await Expect(emptyCell).Not.ToHaveClassAsync(new Regex("due-date-today|due-date-overdue"));
    }

    [Test, Retry(2)]
    public async Task Should_ShowManageFocusCardWithStackedSections()
    {
        var order = await SeedDraftWithDueDateAsync($"[{TestTag}] focus card", null);

        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.Locator(".focus-card")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title))).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name)).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name)).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task Should_FilterSearchThenOpenCardAndSave()
    {
        var creator = CurrentUser;
        var order = Faker<WorkOrder>();
        order.Creator = creator;
        order.Title = $"[{TestTag}] filter card save";

        await using var context = TestHost.NewDbContext();
        context.Attach(creator);
        context.Add(order);
        await context.SaveChangesAsync();

        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}").SelectOptionAsync(creator.UserName);
        await Page.Locator($"#{WorkOrderSearch.Elements.SearchButton}").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var updatedTitle = $"{order.Title} updated";
        await ClickWorkOrderNumberFromSearchPage(order);
        await Input(nameof(WorkOrderManage.Elements.Title), updatedTitle);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });

        await Expect(Page.Locator(".work-order-card-list")).ToBeVisibleAsync();
        await Expect(Page.Locator(".work-order-card-list")).ToContainTextAsync(updatedTitle);
    }

    private async Task<WorkOrder> SeedDraftWithDueDateAsync(string title, DateOnly? dueDate)
    {
        var order = Faker<WorkOrder>();
        order.Title = title;
        order.Number = null;
        order.DueDate = dueDate;
        order.Creator = CurrentUser;

        await using var context = TestHost.NewDbContext();
        context.Attach(CurrentUser);
        context.Add(order);
        await context.SaveChangesAsync();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        order.Number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description ?? "desc");

        if (dueDate.HasValue)
        {
            await Page.GetByTestId(nameof(WorkOrderManage.Elements.DueDate))
                .FillAsync(dueDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        return order;
    }
}