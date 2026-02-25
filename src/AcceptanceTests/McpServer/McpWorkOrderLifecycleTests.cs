using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.IntegrationTests;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.McpAcceptanceTests;

[TestFixture]
public class McpWorkOrderLifecycleTests
{
    private McpTestHelper Helper => McpHttpServerFixture.Helper!;

    [SetUp]
    public void EnsureAvailability()
    {
        if (!McpHttpServerFixture.ServerAvailable)
            Assert.Inconclusive("MCP server is not available");
    }

    private void EnsureLlm()
    {
        if (!McpHttpServerFixture.LlmAvailable)
            Assert.Inconclusive("No LLM available (set AI_OpenAI_ApiKey/Url/Model or run Ollama locally)");
    }

    [Test]
    public async Task ShouldCompleteFullLifecycleViaDirectToolCalls()
    {

        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var creator = employees.First(e => e.Roles.Any(r => r.CanCreateWorkOrder));
        var assignee = employees.First(e =>
            e.Roles.Any(r => r.CanFulfillWorkOrder) && e.UserName != creator.UserName);

        // Step 1: Create a draft work order
        var createResult = await Helper.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = "Lifecycle test work order",
                ["description"] = "Testing full lifecycle via direct MCP tool calls",
                ["creatorUsername"] = creator.UserName!
            });

        createResult.ShouldContain("Lifecycle test work order");
        createResult.ShouldContain("Draft");
        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrEmpty("Work order number should be returned");

        // Step 2: Assign the work order (Draft -> Assigned)
        var assignResult = await Helper.CallToolDirectly("execute-work-order-command",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["commandName"] = "DraftToAssignedCommand",
                ["executingUsername"] = creator.UserName!,
                ["assigneeUsername"] = assignee.UserName!
            });

        assignResult.ShouldContain("Assigned");
        assignResult.ShouldContain(assignee.GetFullName());

        // Step 3: Begin work (Assigned -> InProgress)
        var beginResult = await Helper.CallToolDirectly("execute-work-order-command",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["commandName"] = "AssignedToInProgressCommand",
                ["executingUsername"] = assignee.UserName!
            });

        beginResult.ShouldContain("In Progress");

        // Step 4: Complete work (InProgress -> Complete)
        var completeResult = await Helper.CallToolDirectly("execute-work-order-command",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["commandName"] = "InProgressToCompleteCommand",
                ["executingUsername"] = assignee.UserName!
            });

        completeResult.ShouldContain("Complete");
        completeResult.ShouldContain("CompletedDate");

        // Step 5: Verify final state via get-work-order
        var getResult = await Helper.CallToolDirectly("get-work-order",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber
            });

        getResult.ShouldContain("Complete");
        getResult.ShouldContain("Lifecycle test work order");
        getResult.ShouldContain(creator.GetFullName());
        getResult.ShouldContain(assignee.GetFullName());
    }

    [Test]
    public async Task ShouldAssignAndCancelViaDirectToolCalls()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var creator = employees.First(e => e.Roles.Any(r => r.CanCreateWorkOrder));
        var assignee = employees.First(e =>
            e.Roles.Any(r => r.CanFulfillWorkOrder) && e.UserName != creator.UserName);

        // Create and assign
        var createResult = await Helper.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = "Cancel test work order",
                ["description"] = "Testing cancellation via direct MCP tool calls",
                ["creatorUsername"] = creator.UserName!
            });

        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");

        await Helper.CallToolDirectly("execute-work-order-command",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["commandName"] = "DraftToAssignedCommand",
                ["executingUsername"] = creator.UserName!,
                ["assigneeUsername"] = assignee.UserName!
            });

        // Cancel from Assigned state (creator cancels)
        var cancelResult = await Helper.CallToolDirectly("execute-work-order-command",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["commandName"] = "AssignedToCancelledCommand",
                ["executingUsername"] = creator.UserName!
            });

        cancelResult.ShouldContain("Cancelled");
    }

    [Test]
    public async Task ShouldBeginAndShelveViaDirectToolCalls()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var creator = employees.First(e => e.Roles.Any(r => r.CanCreateWorkOrder));
        var assignee = employees.First(e =>
            e.Roles.Any(r => r.CanFulfillWorkOrder) && e.UserName != creator.UserName);

        // Create, assign, and begin
        var createResult = await Helper.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = "Shelve test work order",
                ["description"] = "Testing shelve via direct MCP tool calls",
                ["creatorUsername"] = creator.UserName!
            });

        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");

        await Helper.CallToolDirectly("execute-work-order-command",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["commandName"] = "DraftToAssignedCommand",
                ["executingUsername"] = creator.UserName!,
                ["assigneeUsername"] = assignee.UserName!
            });

        await Helper.CallToolDirectly("execute-work-order-command",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["commandName"] = "AssignedToInProgressCommand",
                ["executingUsername"] = assignee.UserName!
            });

        // Shelve (InProgress -> Assigned)
        var shelveResult = await Helper.CallToolDirectly("execute-work-order-command",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["commandName"] = "InProgressToAssigned",
                ["executingUsername"] = assignee.UserName!
            });

        shelveResult.ShouldContain("Assigned");
    }

    [Test, Retry(2)]
    public async Task ShouldCompleteFullLifecycleViaLlm()
    {
        EnsureLlm();

        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var creator = employees.First(e => e.Roles.Any(r => r.CanCreateWorkOrder));
        var assignee = employees.First(e =>
            e.Roles.Any(r => r.CanFulfillWorkOrder) && e.UserName != creator.UserName);

        // Create and assign via direct tool calls for reliability
        var createResult = await Helper.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = "LLM lifecycle test",
                ["description"] = "Testing full lifecycle via LLM",
                ["creatorUsername"] = creator.UserName!
            });
        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");

        await Helper.CallToolDirectly("execute-work-order-command",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["commandName"] = "DraftToAssignedCommand",
                ["executingUsername"] = creator.UserName!,
                ["assigneeUsername"] = assignee.UserName!
            });

        // Ask the LLM to begin and complete the work order
        var response = await Helper.SendPrompt(
            $"Work order '{workOrderNumber}' is currently in Assigned status, assigned to '{assignee.UserName}'.\n" +
            $"Do these two steps using the execute-work-order-command tool:\n" +
            $"1. Call execute-work-order-command with workOrderNumber='{workOrderNumber}', commandName='AssignedToInProgressCommand', executingUsername='{assignee.UserName}'\n" +
            $"2. Call execute-work-order-command with workOrderNumber='{workOrderNumber}', commandName='InProgressToCompleteCommand', executingUsername='{assignee.UserName}'\n" +
            $"Report the final status of the work order.");

        response.Text.ShouldNotBeNullOrEmpty();
        var responseText = response.Text.ToLowerInvariant();
        (responseText.Contains("complete") || responseText.Contains("completed"))
            .ShouldBeTrue($"Expected 'complete' status in response: {response.Text}");
    }

    [Test, Retry(2)]
    public async Task ShouldCreateAndAssignWorkOrderViaLlm()
    {
        EnsureLlm();

        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var creator = employees.First(e => e.Roles.Any(r => r.CanCreateWorkOrder));
        var assignee = employees.First(e =>
            e.Roles.Any(r => r.CanFulfillWorkOrder) && e.UserName != creator.UserName);

        var response = await Helper.SendPrompt(
            $"Do these two steps:\n" +
            $"1. Call create-work-order with title='Fix sanctuary lighting', description='Replace burned out bulbs in the sanctuary', creatorUsername='{creator.UserName}'.\n" +
            $"2. Take the work order Number from step 1 and call execute-work-order-command with that workOrderNumber, commandName='DraftToAssignedCommand', executingUsername='{creator.UserName}', assigneeUsername='{assignee.UserName}'.\n" +
            $"Report the final status of the work order.");

        response.Text.ShouldNotBeNullOrEmpty();
        var responseText = response.Text.ToLowerInvariant();
        (responseText.Contains("assigned") || responseText.Contains("assign"))
            .ShouldBeTrue($"Expected 'assigned' status in response: {response.Text}");
    }
}
