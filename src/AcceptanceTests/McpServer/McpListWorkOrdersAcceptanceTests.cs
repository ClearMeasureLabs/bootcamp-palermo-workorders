using System.Text.Json;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.EntityFrameworkCore;
using Login = ClearMeasure.Bootcamp.UI.Shared.Pages.Login;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture, NonParallelizable]
public class McpListWorkOrdersAcceptanceTests : AcceptanceTestBase
{
    private static McpTestHelper? _helper;

    [OneTimeSetUp]
    public async Task McpSetUp()
    {
        _helper = new McpTestHelper(TestHost.GetRequiredService<ChatClientFactory>());
        await _helper.ConnectAsync();
    }

    [OneTimeTearDown]
    public async Task McpTearDown()
    {
        if (_helper != null)
        {
            await _helper.DisposeAsync();
        }
    }

    [SetUp]
    public void EnsureMcpAvailable()
    {
        if (!_helper!.Connected)
        {
            Assert.Inconclusive("MCP server is not available");
        }
    }

    [Test, Retry(2)]
    public async Task ShouldMatchMcpUsernameFiltersToPortalSearch()
    {
        var seededNumbers = await SeedParityWorkOrders();
        await LoginAsTlovejoyAsync();

        var lovejoyNumbers = await CallListWorkOrders(
            new Dictionary<string, object?> { ["creatorUsername"] = "tlovejoy" });
        lovejoyNumbers.ShouldContain(seededNumbers.LovejoyDraft);
        await AssertSearchMatches(lovejoyNumbers, creatorUsername: "tlovejoy");

        var willieNumbers = await CallListWorkOrders(
            new Dictionary<string, object?> { ["assigneeUsername"] = "gwillie" });
        willieNumbers.ShouldContain(seededNumbers.WillieAssigned);
        willieNumbers.ShouldContain(seededNumbers.WillieInProgress);
        await AssertSearchMatches(willieNumbers, assigneeUsername: "gwillie");

        var willieInProgressNumbers = await CallListWorkOrders(
            new Dictionary<string, object?>
            {
                ["status"] = "InProgress",
                ["assigneeUsername"] = "gwillie"
            });
        willieInProgressNumbers.ShouldContain(seededNumbers.WillieInProgress);
        willieInProgressNumbers.ShouldNotContain(seededNumbers.WillieAssigned);
        await AssertSearchMatches(
            willieInProgressNumbers,
            assigneeUsername: "gwillie",
            status: "InProgress");

        var lovejoyDraftNumbers = await CallListWorkOrders(
            new Dictionary<string, object?>
            {
                ["status"] = "Draft",
                ["creatorUsername"] = "tlovejoy"
            });
        lovejoyDraftNumbers.ShouldContain(seededNumbers.LovejoyDraft);
        lovejoyDraftNumbers.ShouldNotContain(seededNumbers.WillieAssigned);
        lovejoyDraftNumbers.ShouldNotContain(seededNumbers.LovejoyComplete);
        await AssertSearchMatches(
            lovejoyDraftNumbers,
            creatorUsername: "tlovejoy",
            status: "Draft");

        var allNumbers = await CallListWorkOrders([]);
        allNumbers.ShouldNotBeEmpty();
        await AssertSearchMatches(allNumbers);

        var searchNumbersBeforeUnknown = await VisibleSearchNumbers();
        var unknownNumbers = await CallListWorkOrders(
            new Dictionary<string, object?> { ["creatorUsername"] = "not-a-person" });
        unknownNumbers.ShouldBeEmpty();
        (await VisibleSearchNumbers()).ShouldBeSet(searchNumbersBeforeUnknown);
        await using (var context = TestHost.NewDbContext())
        {
            (await context.Set<Employee>().AnyAsync(employee => employee.UserName == "not-a-person"))
                .ShouldBeFalse();
        }

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + seededNumbers.LovejoyDraft);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber)))
            .ToHaveTextAsync(seededNumbers.LovejoyDraft);
        var creator = Page.Locator(".form-group")
            .Filter(new LocatorFilterOptions { HasText = "Creator:" })
            .Locator(".value");
        await Expect(creator).ToHaveTextAsync("Timothy Lovejoy Jr");
    }

    private async Task<ParityWorkOrderNumbers> SeedParityWorkOrders()
    {
        await using var context = TestHost.NewDbContext();
        var lovejoy = await context.Set<Employee>().SingleAsync(employee => employee.UserName == "tlovejoy");
        var willie = await context.Set<Employee>().SingleAsync(employee => employee.UserName == "gwillie");
        var otherCreator = await context.Set<Employee>().SingleAsync(employee => employee.UserName == "nflanders");
        var otherAssignee = await context.Set<Employee>().SingleAsync(employee => employee.UserName == "mflanders");
        var numbers = new ParityWorkOrderNumbers(
            Number("LJD"),
            Number("WA"),
            Number("WI"),
            Number("LJC"));

        context.AddRange(
            CreateWorkOrder(numbers.LovejoyDraft, lovejoy, otherAssignee, WorkOrderStatus.Draft),
            CreateWorkOrder(Number("OD"), otherCreator, otherAssignee, WorkOrderStatus.Draft),
            CreateWorkOrder(numbers.WillieAssigned, lovejoy, willie, WorkOrderStatus.Assigned),
            CreateWorkOrder(numbers.WillieInProgress, lovejoy, willie, WorkOrderStatus.InProgress),
            CreateWorkOrder(Number("OWI"), otherCreator, willie, WorkOrderStatus.InProgress),
            CreateWorkOrder(numbers.LovejoyComplete, lovejoy, otherAssignee, WorkOrderStatus.Complete));
        await context.SaveChangesAsync();
        return numbers;
    }

    private string Number(string role)
    {
        var number = $"{TestTag[..4]}{role}";
        number.Length.ShouldBeLessThanOrEqualTo(7);
        return number;
    }

    private static WorkOrder CreateWorkOrder(
        string number,
        Employee creator,
        Employee assignee,
        WorkOrderStatus status) =>
        new()
        {
            Number = number,
            Title = $"MCP list parity {number}",
            Description = "MCP list parity acceptance test",
            Creator = creator,
            Assignee = assignee,
            Status = status
        };

    private static async Task<HashSet<string>> CallListWorkOrders(Dictionary<string, object?> arguments)
    {
        var json = await _helper!.CallToolDirectly("list-work-orders", arguments);
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateArray()
            .Select(item => item.GetProperty("Number").GetString()!)
            .ToHashSet();
    }

    private async Task AssertSearchMatches(
        HashSet<string> expectedNumbers,
        string? creatorUsername = null,
        string? assigneeUsername = null,
        string? status = null)
    {
        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}");

        await creatorSelect.SelectOptionAsync(creatorUsername ?? string.Empty);
        await assigneeSelect.SelectOptionAsync(assigneeUsername ?? string.Empty);
        await statusSelect.SelectOptionAsync(status ?? string.Empty);
        await Page.Locator($"#{WorkOrderSearch.Elements.SearchButton}").ClickAsync();

        var workOrderLinks = Page.Locator($"[data-testid^='{WorkOrderSearch.Elements.WorkOrderLink}']");
        await Expect(workOrderLinks).ToHaveCountAsync(expectedNumbers.Count);
        foreach (var number in expectedNumbers)
        {
            await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + number))
                .ToBeVisibleAsync();
        }

        (await VisibleSearchNumbers()).ShouldBeSet(expectedNumbers);
    }

    private async Task<HashSet<string>> VisibleSearchNumbers()
    {
        var workOrderLinks = Page.Locator($"[data-testid^='{WorkOrderSearch.Elements.WorkOrderLink}']");
        return (await workOrderLinks.AllTextContentsAsync())
            .Select(number => number.Trim())
            .ToHashSet();
    }

    private async Task LoginAsTlovejoyAsync()
    {
        await Page.GotoAsync("/login");
        await Expect(Page.GetByTestId(nameof(Login.Elements.LovejoyShortcut))).ToBeVisibleAsync();
        await Click(nameof(Login.Elements.LovejoyShortcut));
        await Expect(Page.GetByTestId(nameof(Logout.Elements.WelcomeText)))
            .ToHaveTextAsync("Welcome tlovejoy!");
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.SearchButton}")).ToBeVisibleAsync();
    }

    private sealed record ParityWorkOrderNumbers(
        string LovejoyDraft,
        string WillieAssigned,
        string WillieInProgress,
        string LovejoyComplete);
}

internal static class McpListWorkOrderNumberAssertions
{
    public static void ShouldBeSet(this IEnumerable<string> actual, IEnumerable<string> expected)
    {
        actual.OrderBy(number => number).ShouldBe(expected.OrderBy(number => number));
    }
}
