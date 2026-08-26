using System.Globalization;
using System.Text.Json;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Login = ClearMeasure.Bootcamp.UI.Shared.Pages.Login;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpGetWorkOrderInstructionsAcceptanceTests : AcceptanceTestBase
{
    private static McpTestHelper? _helper;
    private McpTestHelper Helper =>
        _helper ?? throw new InvalidOperationException("MCP helper is not initialized.");

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
        if (!Helper.Connected)
        {
            Assert.Inconclusive("MCP server is not available");
        }
    }

    [Test, Retry(2)]
    public async Task ShouldMatchGetWorkOrderDetailWithManageWhenInstructionsSet()
    {
        await LoginAsTlovejoyAsync();

        const string title = "Saturday mow";
        const string description = "Mow the front lawn";
        const string instructions = "preschool quiet";
        const string room = "Front lawn";
        const string dueDate = "2026-09-12";
        var createResult = await Helper.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = title,
                ["description"] = description,
                ["creatorUsername"] = "tlovejoy",
                ["roomNumber"] = room,
                ["dueDate"] = dueDate,
                ["instructions"] = instructions
            });
        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrWhiteSpace();

        var getResult = await GetWorkOrderAsync(workOrderNumber);
        using var document = JsonDocument.Parse(getResult);
        var detail = document.RootElement;

        RequiredString(detail, "Instructions").ShouldBe(instructions);
        RequiredString(detail, "Title").ShouldBe(title);
        RequiredString(detail, "Description").ShouldBe(description);
        RequiredString(detail, "RoomNumber").ShouldBe(room);
        RequiredString(detail, "DueDate").ShouldBe(dueDate);
        RequiredString(detail, "Status").ShouldBe("Draft");
        RequiredString(detail, "CreatorUsername").ShouldBe("tlovejoy");
        detail.GetProperty("Assignee").ValueKind.ShouldBe(JsonValueKind.Null);
        detail.GetProperty("AssigneeUsername").ValueKind.ShouldBe(JsonValueKind.Null);
        detail.GetProperty("AssignedDate").ValueKind.ShouldBe(JsonValueKind.Null);
        detail.GetProperty("CompletedDate").ValueKind.ShouldBe(JsonValueKind.Null);

        await NavigateToManageEditAsync(workOrderNumber);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)))
            .ToHaveValueAsync(RequiredString(detail, "Title"));
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description)))
            .ToHaveValueAsync(RequiredString(detail, "Description"));
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions)))
            .ToHaveValueAsync(RequiredString(detail, "Instructions"));
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber)))
            .ToHaveValueAsync(RequiredString(detail, "RoomNumber"));
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.DueDate)))
            .ToHaveValueAsync(RequiredString(detail, "DueDate"));
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(RequiredString(detail, "Status"));
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee)))
            .ToHaveValueAsync(string.Empty);

        var creator = RequiredString(detail, "Creator");
        var creatorGroup = Page.Locator(".form-group").Filter(new LocatorFilterOptions { HasText = "Creator:" });
        await Expect(creatorGroup.GetByText(creator, new LocatorGetByTextOptions { Exact = true }))
            .ToBeVisibleAsync();

        var createdDate = detail.GetProperty("CreatedDate").GetDateTime();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.CreatedDate)))
            .ToHaveTextAsync(createdDate.ToString("G", CultureInfo.CurrentCulture));
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.AssignedDate)))
            .ToHaveTextAsync(string.Empty);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.CompletedDate)))
            .ToHaveTextAsync(string.Empty);

        var unknownResult = await GetWorkOrderAsync("ZZ9103");
        unknownResult.ShouldBe("No work order found with number 'ZZ9103'.");
    }

    [Test, Retry(2)]
    public async Task ShouldMatchEmptyManageInstructionsWithPresentEmptyGetProperty()
    {
        await LoginAsTlovejoyAsync();

        var createResult = await Helper.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = "No special instructions",
                ["description"] = "Instructions intentionally omitted",
                ["creatorUsername"] = "tlovejoy"
            });
        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrWhiteSpace();

        var getResult = await GetWorkOrderAsync(workOrderNumber);
        using var document = JsonDocument.Parse(getResult);
        var instructions = document.RootElement.GetProperty("Instructions");
        instructions.ValueKind.ShouldBe(JsonValueKind.String);
        instructions.GetString().ShouldBe(string.Empty);
        getResult.ShouldContain("\"Instructions\": \"\"");
        getResult.ShouldNotContain("\"Instructions\": \"null\"");

        await NavigateToManageEditAsync(workOrderNumber);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions)))
            .ToHaveValueAsync(string.Empty);
    }

    private static string RequiredString(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).GetString()
        ?? throw new InvalidOperationException($"Property '{propertyName}' was null.");

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
        var workOrderNumberField = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(workOrderNumberField)
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        await Expect(workOrderNumberField).ToHaveTextAsync(workOrderNumber);
    }

    private Task<string> GetWorkOrderAsync(string workOrderNumber) =>
        Helper.CallToolDirectly("get-work-order",
            new Dictionary<string, object?> { ["workOrderNumber"] = workOrderNumber });
}
