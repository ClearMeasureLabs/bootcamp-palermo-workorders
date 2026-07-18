using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.IntegrationTests;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpWorkRequestLifecycleTests : AcceptanceTestBase
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
    public void EnsureAvailability()
    {
        if (!_helper!.Connected)
            Assert.Inconclusive("MCP server is not available");
    }

    [Test]
    public async Task ShouldCompleteFullLifecycleViaDirectToolCalls()
    {

        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var creator = employees.First(e => e.Roles.Any(r => r.CanCreateWorkRequest));
        var assignee = employees.First(e =>
            e.Roles.Any(r => r.CanFulfillWorkRequest) && e.UserName != creator.UserName);

        // Step 1: Create a draft work request
        var createResult = await _helper!.CallToolDirectly("create-work-request",
            new Dictionary<string, object?>
            {
                ["title"] = "Lifecycle test work request",
                ["description"] = "Testing full lifecycle via direct MCP tool calls",
                ["creatorUsername"] = creator.UserName!
            });

        createResult.ShouldContain("Lifecycle test work request");
        createResult.ShouldContain("Draft");
        var workRequestNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workRequestNumber.ShouldNotBeNullOrEmpty("Work request number should be returned");

        // Step 2: Assign the work request (Draft -> Assigned)
        var assignResult = await _helper!.CallToolDirectly("execute-work-request-command",
            new Dictionary<string, object?>
            {
                ["workRequestNumber"] = workRequestNumber,
                ["commandName"] = "DraftToAssignedCommand",
                ["executingUsername"] = creator.UserName!,
                ["assigneeUsername"] = assignee.UserName!
            });

        assignResult.ShouldContain("Assigned");
        assignResult.ShouldContain(assignee.GetFullName());

        // Step 3: Begin work (Assigned -> InProgress)
        var beginResult = await _helper!.CallToolDirectly("execute-work-request-command",
            new Dictionary<string, object?>
            {
                ["workRequestNumber"] = workRequestNumber,
                ["commandName"] = "AssignedToInProgressCommand",
                ["executingUsername"] = assignee.UserName!
            });

        beginResult.ShouldContain("In Progress");

        // Step 4: Complete work (InProgress -> Complete)
        var completeResult = await _helper!.CallToolDirectly("execute-work-request-command",
            new Dictionary<string, object?>
            {
                ["workRequestNumber"] = workRequestNumber,
                ["commandName"] = "InProgressToCompleteCommand",
                ["executingUsername"] = assignee.UserName!
            });

        completeResult.ShouldContain("Complete");
        completeResult.ShouldContain("CompletedDate");

        // Step 5: Verify final state via get-work-request
        var getResult = await _helper!.CallToolDirectly("get-work-request",
            new Dictionary<string, object?>
            {
                ["workRequestNumber"] = workRequestNumber
            });

        getResult.ShouldContain("Complete");
        getResult.ShouldContain("Lifecycle test work request");
        getResult.ShouldContain(creator.GetFullName());
        getResult.ShouldContain(assignee.GetFullName());
    }

}
