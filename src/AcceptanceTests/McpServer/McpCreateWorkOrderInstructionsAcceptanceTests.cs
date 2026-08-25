using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Login = ClearMeasure.Bootcamp.UI.Shared.Pages.Login;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpCreateWorkOrderInstructionsAcceptanceTests : AcceptanceTestBase
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
    public async Task ShouldShowExplicitInstructionsOnManageEditAfterMcpCreate()
    {
        await LoginAsTlovejoyAsync();

        const string description = "Repair fellowship hall window latch";
        var instructions = $"[{TestTag}] Enter through north door; ladder in east shed";
        var createResult = await _helper!.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = $"[{TestTag}] MCP instructions explicit",
                ["description"] = description,
                ["creatorUsername"] = "tlovejoy",
                ["instructions"] = instructions
            });

        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrWhiteSpace();

        await NavigateToManageEditAsync(workOrderNumber);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(instructions);

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        await Expect(descriptionField).ToHaveValueAsync(description);
        await Expect(instructionsField).Not.ToHaveValueAsync(description);
    }

    [Test, Retry(2)]
    public async Task ShouldSaveSuccessfullyWhenInstructionsOmittedAfterMcpCreate()
    {
        await LoginAsTlovejoyAsync();

        var createResult = await _helper!.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = $"[{TestTag}] MCP instructions omitted",
                ["description"] = "Omitted instructions scenario",
                ["creatorUsername"] = "tlovejoy"
            });

        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrWhiteSpace();

        await NavigateToManageEditAsync(workOrderNumber);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(string.Empty);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });

        await NavigateToManageEditAsync(workOrderNumber);
        await Expect(instructionsField).ToHaveValueAsync(string.Empty);
    }

    [Test, Retry(2)]
    public async Task ShouldSaveSuccessfullyWhenInstructionsEmptyAfterMcpCreate()
    {
        await LoginAsTlovejoyAsync();

        var createResult = await _helper!.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = $"[{TestTag}] MCP instructions empty",
                ["description"] = "Empty instructions scenario",
                ["creatorUsername"] = "tlovejoy",
                ["instructions"] = string.Empty
            });

        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrWhiteSpace();

        await NavigateToManageEditAsync(workOrderNumber);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(string.Empty);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });

        await NavigateToManageEditAsync(workOrderNumber);
        await Expect(instructionsField).ToHaveValueAsync(string.Empty);
    }

    [Test, Retry(2)]
    public async Task ShouldTruncate4001CharacterInstructionsOnManageEditWithoutValidationBanner()
    {
        await LoginAsTlovejoyAsync();

        var longInstructions = new string('M', WorkOrder.InstructionsMaxLength + 1);
        var expectedInstructions = new string('M', WorkOrder.InstructionsMaxLength);

        var createResult = await _helper!.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = $"[{TestTag}] MCP instructions truncate",
                ["description"] = "Truncation scenario",
                ["creatorUsername"] = "tlovejoy",
                ["instructions"] = longInstructions
            });

        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrWhiteSpace();

        await NavigateToManageEditAsync(workOrderNumber);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(expectedInstructions);
        await Expect(Page.GetByText("Instructions cannot exceed 4000 characters.")).Not.ToBeVisibleAsync();
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
}
