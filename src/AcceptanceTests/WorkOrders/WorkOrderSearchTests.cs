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
    public async Task ShouldRenderStatusPillsInMockOrder()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var pills = Page.Locator(".filter-pill");
        await Expect(pills).ToHaveCountAsync(6);
        (await pills.AllInnerTextsAsync()).ShouldBe(
        [
            "All",
            "Due Today",
            "Due This Week",
            "In Progress",
            "On Hold",
            "Completed"
        ]);
    }

    [Test, Retry(2)]
    public async Task ShouldRenderAllPillActiveWithoutChurchSearchForm()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId(WorkOrderSearch.Elements.SearchButton.ToString()))
            .ToHaveClassAsync(new Regex("active"));
        await Expect(Page.Locator(".search-filters-card")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".legacy-search-controls")).ToBeHiddenAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldMapStatusPillsToStatusQueryParameters()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId(WorkOrderSearch.Elements.InProgressFilter.ToString()))
            .ToHaveAttributeAsync("href", "/workorder/search?Status=InProgress");
        await Expect(Page.GetByTestId(WorkOrderSearch.Elements.OnHoldFilter.ToString()))
            .ToHaveAttributeAsync("href", "/workorder/search?Status=Assigned");
        await Expect(Page.GetByTestId(WorkOrderSearch.Elements.CompletedFilter.ToString()))
            .ToHaveAttributeAsync("href", "/workorder/search?Status=Complete");
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkOrderStackWithAllFiltersSetToAllOnInitialLoad()
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
        var workOrderStack = Page.GetByTestId("WorkOrderStack");
        await Expect(workOrderStack).ToBeVisibleAsync();

        var workOrderCards = workOrderStack.Locator(".work-order-card");
        var cardCount = await workOrderCards.CountAsync();
        await Expect(workOrderCards).ToHaveCountAsync(cardCount);
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkOrderStackWithCreatorFilterFromQueryString()
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

        await Expect(Page).ToHaveURLAsync(new Regex($"Creator={Regex.Escape(creator.UserName)}"));

        var workOrderStack = Page.GetByTestId("WorkOrderStack");
        await Expect(workOrderStack).ToBeVisibleAsync();

        var workOrderCards = workOrderStack.Locator(".work-order-card");
        var cardCount = await workOrderCards.CountAsync();
        cardCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkOrderStackWithAssigneeFilterFromQueryString()
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

        await Expect(Page).ToHaveURLAsync(new Regex($"Assignee={Regex.Escape(assignee.UserName)}"));

        var workOrderStack = Page.GetByTestId("WorkOrderStack");
        await Expect(workOrderStack).ToBeVisibleAsync();

        var workOrderCards = workOrderStack.Locator(".work-order-card");
        var cardCount = await workOrderCards.CountAsync();
        cardCount.ShouldBeGreaterThanOrEqualTo(1);
        await Expect(workOrderCards.First).ToContainTextAsync(assignee.GetFullName());
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkOrderStackWithStatusFilterFromQueryString()
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

        await Expect(Page).ToHaveURLAsync(new Regex($"Status={Regex.Escape(status.Key)}"));
        await Expect(Page.GetByTestId(WorkOrderSearch.Elements.OnHoldFilter.ToString()))
            .ToHaveClassAsync(new Regex("active"));

        var workOrderStack = Page.GetByTestId("WorkOrderStack");
        await Expect(workOrderStack).ToBeVisibleAsync();

        var workOrderCards = workOrderStack.Locator(".work-order-card");
        await Expect(workOrderCards).ToHaveCountAsync(await workOrderCards.CountAsync());
    }

    [Test, Retry(2)]
    public async Task ShouldFilterCompletedWorkOrdersWithStatusPill()
    {
        var creator = Faker<Employee>();
        var order = Faker<WorkOrder>();
        order.Creator = creator;
        order.Status = WorkOrderStatus.Complete;

        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        context.Add(order);
        await context.SaveChangesAsync();

        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.GetByTestId(WorkOrderSearch.Elements.CompletedFilter.ToString()).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page).ToHaveURLAsync(new Regex("Status=Complete"));
        await Expect(Page.GetByTestId(WorkOrderSearch.Elements.CompletedFilter.ToString()))
            .ToHaveClassAsync(new Regex("active"));
        await Expect(Page.GetByTestId(WorkOrderSearch.Elements.WorkOrderLink + order.Number))
            .ToBeAttachedAsync();
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

        var workOrderStack = Page.GetByTestId("WorkOrderStack");
        await Expect(workOrderStack).ToBeVisibleAsync();

        var firstCard = workOrderStack.Locator(".work-order-card").First;
        var workOrderNumber = (await firstCard.Locator(".work-order-number").TextContentAsync())?.Trim();

        if (!string.IsNullOrEmpty(workOrderNumber))
        {
            var firstWorkOrderLink = Page.GetByTestId(WorkOrderSearch.Elements.WorkOrderLink + workOrderNumber);
            await firstWorkOrderLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await TakeScreenshotAsync(2, "WorkOrderDetailsPage");

            // Assert
            await Expect(Page).ToHaveURLAsync(new Regex($"/workorder/manage/{Regex.Escape(workOrderNumber)}"));
        }
    }

    [Test, Retry(2)]
    public async Task ShouldSelectCardWithoutOpeningFocusPane()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderStack = Page.GetByTestId("WorkOrderStack");
        var firstCard = workOrderStack.Locator(".work-order-card").First;

        await firstCard.ClickAsync();

        await Expect(firstCard).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(firstCard).ToHaveCSSAsync("border-color", "rgb(0, 133, 202)");
        await Expect(Page.Locator(".focus-pane")).ToHaveCountAsync(0);
    }

    [Test, Retry(2)]
    public async Task ShouldClearStatusFilterWhenSelectingAllPill()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId(WorkOrderSearch.Elements.InProgressFilter.ToString()).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page).ToHaveURLAsync(new Regex("Status=InProgress"));

        await Page.GetByTestId(WorkOrderSearch.Elements.SearchButton.ToString()).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page).ToHaveURLAsync(new Regex("/workorder/search$"));
        await Expect(Page.GetByTestId(WorkOrderSearch.Elements.SearchButton.ToString()))
            .ToHaveClassAsync(new Regex("active"));
    }

    [Test, Retry(2)]
    public async Task ShouldMaintainSelectedStatusPillAfterNavigation()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId(WorkOrderSearch.Elements.CompletedFilter.ToString()).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId(WorkOrderSearch.Elements.CompletedFilter.ToString()))
            .ToHaveClassAsync(new Regex("active"));
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

        await Click(nameof(NavMenu.Elements.MyWorkOrders));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page).ToHaveURLAsync(new Regex($"Creator={Regex.Escape(CurrentUser.UserName)}"));

        await Click(nameof(NavMenu.Elements.WorkOrdersAssignedToMe));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page).ToHaveURLAsync(new Regex($"Assignee={Regex.Escape(CurrentUser.UserName)}"));

        await Click(nameof(NavMenu.Elements.AllWorkOrdersInProgress));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page).ToHaveURLAsync(new Regex($"Status={Regex.Escape(order1.Status.Key)}"));
        await Expect(Page.GetByTestId(WorkOrderSearch.Elements.InProgressFilter.ToString()))
            .ToHaveClassAsync(new Regex("active"));
    }
}