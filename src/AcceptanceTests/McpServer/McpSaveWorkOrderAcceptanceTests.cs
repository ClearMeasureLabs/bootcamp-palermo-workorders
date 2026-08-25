using System.Globalization;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Login = ClearMeasure.Bootcamp.UI.Shared.Pages.Login;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpSaveWorkOrderAcceptanceTests : AcceptanceTestBase
{
    private const string DueTodayBackground = "rgb(254, 240, 138)";

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
    public async Task ShouldShowSavedTitleAndClearedDueDateOnManageAndSearch()
    {
        await LoginAsTlovejoyAsync();

        const string instructions = "Watch for preschool play area";
        const string room = "Front lawn";
        const string dueDate = "2026-09-12";
        var createResult = await _helper!.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = $"[{TestTag}] mow front grass",
                ["description"] = "Weekly mowing task",
                ["creatorUsername"] = "tlovejoy",
                ["instructions"] = instructions,
                ["roomNumber"] = room,
                ["dueDate"] = dueDate
            });

        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrWhiteSpace();

        var saveResult = await _helper.CallToolDirectly("save-work-order",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["executingUsername"] = "tlovejoy",
                ["title"] = "Saturday mow",
                ["dueDate"] = string.Empty
            });

        saveResult.ShouldContain("Saturday mow");

        await NavigateToManageEditAsync(workOrderNumber);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)))
            .ToHaveValueAsync("Saturday mow");
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.DueDate)))
            .ToHaveValueAsync(string.Empty);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions)))
            .ToHaveValueAsync(instructions);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber)))
            .ToHaveValueAsync(room);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync("Draft");

        await NavigateToSearchAsync();

        var row = SearchRowForWorkOrder(workOrderNumber);
        await Expect(row).ToContainTextAsync("Saturday mow");

        var dueDateCell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.DueDateCell) + workOrderNumber);
        await Expect(dueDateCell).ToBeAttachedAsync();
        await Expect(dueDateCell).ToHaveTextAsync(string.Empty);
        await Expect(dueDateCell).Not.ToHaveClassAsync(new Regex("due-date-today|due-date-overdue"));
    }

    [Test, Retry(2)]
    public async Task ShouldShowChicagoTodayDueDateWithYellowOnManageAndSearch()
    {
        await LoginAsTlovejoyAsync();

        var createResult = await _helper!.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = $"[{TestTag}] due today via save",
                ["description"] = "Due date coloring scenario",
                ["creatorUsername"] = "tlovejoy"
            });

        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrWhiteSpace();

        var today = ChurchTimeZone.Today(TimeProvider.System);
        var todayText = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        await _helper.CallToolDirectly("save-work-order",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["executingUsername"] = "tlovejoy",
                ["dueDate"] = todayText
            });

        await NavigateToManageEditAsync(workOrderNumber);

        var dueDateInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.DueDate));
        await Expect(dueDateInput).ToHaveValueAsync(todayText);
        (await BackgroundColorAsync(dueDateInput)).ShouldBe(DueTodayBackground);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync("Draft");

        await NavigateToSearchAsync();

        var dueDateCell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.DueDateCell) + workOrderNumber);
        await Expect(dueDateCell).ToBeAttachedAsync(new LocatorAssertionsToBeAttachedOptions { Timeout = 30_000 });
        await Expect(dueDateCell).ToHaveClassAsync(new Regex("due-date-today"));
        (await BackgroundColorAsync(dueDateCell)).ShouldBe(DueTodayBackground);
    }

    [Test, Retry(2)]
    public async Task ShouldTruncate4001CharacterInstructionsOnManageEditWithoutValidationBanner()
    {
        await LoginAsTlovejoyAsync();

        var createResult = await _helper!.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = $"[{TestTag}] save truncate instructions",
                ["description"] = "Truncation via save-work-order",
                ["creatorUsername"] = "tlovejoy"
            });

        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrWhiteSpace();

        var longInstructions = new string('S', WorkOrder.InstructionsMaxLength + 1);
        var expectedInstructions = new string('S', WorkOrder.InstructionsMaxLength);

        await _helper.CallToolDirectly("save-work-order",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["executingUsername"] = "tlovejoy",
                ["instructions"] = longInstructions
            });

        await NavigateToManageEditAsync(workOrderNumber);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(expectedInstructions);
        await Expect(Page.GetByText("Instructions cannot exceed 4000 characters.")).Not.ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldFailSaveForAssignedWorkOrderWithoutChangingManageFields()
    {
        await LoginAsTlovejoyAsync();

        const string originalTitle = "Assigned save guard";
        var createResult = await _helper!.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = $"[{TestTag}] {originalTitle}",
                ["description"] = "Assigned scenario",
                ["creatorUsername"] = "tlovejoy"
            });

        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrWhiteSpace();

        await _helper.CallToolDirectly("execute-work-order-command",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["commandName"] = "DraftToAssignedCommand",
                ["executingUsername"] = "tlovejoy",
                ["assigneeUsername"] = "gwillie"
            });

        var saveResult = await _helper.CallToolDirectly("save-work-order",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["executingUsername"] = "tlovejoy",
                ["title"] = "Should not persist"
            });

        saveResult.ShouldContain("cannot be executed");

        await NavigateToManageEditAsync(workOrderNumber);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)))
            .ToHaveValueAsync($"[{TestTag}] {originalTitle}");
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync("Assigned");
    }

    [Test, Retry(2)]
    public async Task ShouldFailSaveForNonCreatorWithoutChangingManageFields()
    {
        await LoginAsTlovejoyAsync();

        const string originalTitle = "Non-creator save guard";
        var createResult = await _helper!.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = $"[{TestTag}] {originalTitle}",
                ["description"] = "Non-creator scenario",
                ["creatorUsername"] = "tlovejoy"
            });

        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrWhiteSpace();

        var saveResult = await _helper.CallToolDirectly("save-work-order",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["executingUsername"] = "gwillie",
                ["title"] = "Should not persist"
            });

        saveResult.ShouldContain("cannot be executed");

        await NavigateToManageEditAsync(workOrderNumber);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)))
            .ToHaveValueAsync($"[{TestTag}] {originalTitle}");
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync("Draft");
    }

    private async Task LoginAsTlovejoyAsync()
    {
        await Page.GotoAsync("/login");
        var shortcut = Page.GetByTestId(nameof(Login.Elements.LovejoyShortcut));
        await shortcut.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000
        });
        await Click(nameof(Login.Elements.LovejoyShortcut));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByTestId(nameof(Logout.Elements.WelcomeText)))
            .ToHaveTextAsync("Welcome tlovejoy!");
    }

    private async Task NavigateToManageEditAsync(string workOrderNumber)
    {
        await Page.GotoAsync($"/workorder/manage/{workOrderNumber}?mode=Edit");
        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        await Expect(woNumberLocator).ToHaveTextAsync(workOrderNumber);
    }

    private async Task NavigateToSearchAsync()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private ILocator SearchRowForWorkOrder(string workOrderNumber)
    {
        var link = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + workOrderNumber);
        return Page.Locator("tr").Filter(new LocatorFilterOptions { Has = link });
    }

    private static async Task<string> BackgroundColorAsync(ILocator locator)
    {
        return await locator.EvaluateAsync<string>(
            "element => window.getComputedStyle(element).backgroundColor");
    }
}
