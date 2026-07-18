using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.IntegrationTests;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpWorkRequestLifecycleLlmTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

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
        if (_helper != null) await _helper.DisposeAsync();
    }

    [SetUp]
    public async Task EnsureAvailability()
    {
        if (!_helper!.Connected)
            Assert.Inconclusive("MCP server is not available");
        await SkipIfNoChatClient();
    }

    [Test, Retry(2)]
    public async Task ShouldCompleteFullLifecycleViaLlm()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var creator = employees.First(e => e.Roles.Any(r => r.CanCreateWorkRequest));
        var assignee = employees.First(e =>
            e.Roles.Any(r => r.CanFulfillWorkRequest) && e.UserName != creator.UserName);

        // Create and assign via direct tool calls for reliability
        var createResult = await _helper!.CallToolDirectly("create-work-request",
            new Dictionary<string, object?>
            {
                ["title"] = "LLM lifecycle test",
                ["description"] = "Testing full lifecycle via LLM",
                ["creatorUsername"] = creator.UserName!
            });
        var workRequestNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");

        await _helper!.CallToolDirectly("execute-work-request-command",
            new Dictionary<string, object?>
            {
                ["workRequestNumber"] = workRequestNumber,
                ["commandName"] = "DraftToAssignedCommand",
                ["executingUsername"] = creator.UserName!,
                ["assigneeUsername"] = assignee.UserName!
            });

        // Ask the LLM to begin and complete the work request
        var response = await _helper!.SendPrompt(
            $"Work request '{workRequestNumber}' is currently in Assigned status, assigned to '{assignee.UserName}'.\n" +
            $"Do these two steps using the execute-work-request-command tool:\n" +
            $"1. Call execute-work-request-command with workRequestNumber='{workRequestNumber}', commandName='AssignedToInProgressCommand', executingUsername='{assignee.UserName}'\n" +
            $"2. Call execute-work-request-command with workRequestNumber='{workRequestNumber}', commandName='InProgressToCompleteCommand', executingUsername='{assignee.UserName}'\n" +
            $"Report the final status of the work request.");

        response.Text.ShouldNotBeNullOrEmpty();
        var responseText = response.Text.ToLowerInvariant();
        (responseText.Contains("complete") || responseText.Contains("completed"))
            .ShouldBeTrue($"Expected 'complete' status in response: {response.Text}");
    }

    [Test, Retry(2)]
    public async Task ShouldCreateAndAssignWorkRequestViaLlm()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var creator = employees.First(e => e.Roles.Any(r => r.CanCreateWorkRequest));
        var assignee = employees.First(e =>
            e.Roles.Any(r => r.CanFulfillWorkRequest) && e.UserName != creator.UserName);

        var response = await _helper!.SendPrompt(
            $"Do these two steps:\n" +
            $"1. Call create-work-request with title='Fix sanctuary lighting', description='Replace burned out bulbs in the sanctuary', creatorUsername='{creator.UserName}'.\n" +
            $"2. Take the work request Number from step 1 and call execute-work-request-command with that workRequestNumber, commandName='DraftToAssignedCommand', executingUsername='{creator.UserName}', assigneeUsername='{assignee.UserName}'.\n" +
            $"Report the final status of the work request.");

        response.Text.ShouldNotBeNullOrEmpty();
        var responseText = response.Text.ToLowerInvariant();
        (responseText.Contains("assigned") || responseText.Contains("assign"))
            .ShouldBeTrue($"Expected 'assigned' status in response: {response.Text}");
    }
}
