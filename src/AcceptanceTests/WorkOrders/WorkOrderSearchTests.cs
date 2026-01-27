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

    [Test]
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

        var employeeCount = (await Bus.Send(new EmployeeGetAllQuery())).Length;
        var creatorOptions = creatorSelect.Locator("option");
        await Expect(creatorOptions).ToHaveCountAsync(employeeCount + 1); //including the "all" option
        await Expect(creatorOptions.First).ToHaveTextAsync("All");

        var assigneeOptions = assigneeSelect.Locator("option");
        await Expect(assigneeOptions).ToHaveCountAsync(employeeCount + 1);
        await Expect(assigneeOptions.First).ToHaveTextAsync("All");

        // Verify status options are loaded (5 statuses + "All" option = 6 options)
        var statusOptions = statusSelect.Locator("option");
        await Expect(statusOptions).ToHaveCountAsync(WorkOrderStatus.GetAllItems().Length + 1);
        await Expect(statusOptions.First).ToHaveTextAsync("All");
    }

    [Test]
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

    [Test]
    public async Task ShouldLoadWorkOrderTableWithCreatorFilterFromQueryString()
    {
        // Arrange
        var creator = CurrentUser;
        var order = Faker<WorkOrder>();
        order.Creator = creator;
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
        await Expect(workOrderRows).ToHaveCountAsync(1);
        await Expect(workOrderRows.First.Locator("td:nth-child(2)")).ToContainTextAsync(creator.GetFullName());
    }

    [Test]
    public async Task ShouldLoadWorkOrderTableWithAssigneeFilterFromQueryString()
    {
        // Arrange
        var creator = Faker<Employee>();
        var assignee = CurrentUser;
        var order = Faker<WorkOrder>();
        order.Creator = creator;
        order.Assignee = assignee;

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
        await Expect(workOrderRows).ToHaveCountAsync(1);
        await Expect(workOrderRows.First.Locator("td:nth-child(3)")).ToContainTextAsync(assignee.GetFullName());
    }

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
    public async Task SearchWorkOrder_WithLongTitle_FindsAndDisplaysCorrectly()
    {
        // Arrange
        var creator = Faker<Employee>();
        var longTitle = new string('Z', 650) + "UNIQUE_SEARCH_TERM" + new string('Y', 33); // Total 700 chars
        var workOrder = Faker<WorkOrder>();
        workOrder.Creator = creator;
        workOrder.Title = longTitle;

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

        // Find the work order with the unique search term in its title
        var workOrderRows = workOrderTable.Locator("tbody tr");
        var rowCount = await workOrderRows.CountAsync();
        bool foundWorkOrder = false;
        
        for (int i = 0; i < rowCount; i++)
        {
            var row = workOrderRows.Nth(i);
            var titleCell = row.Locator("td").Nth(1);
            var titleText = await titleCell.TextContentAsync();
            
            if (titleText != null && titleText.Contains("UNIQUE_SEARCH_TERM"))
            {
                foundWorkOrder = true;
                break;
            }
        }

        foundWorkOrder.ShouldBeTrue();
    }
}