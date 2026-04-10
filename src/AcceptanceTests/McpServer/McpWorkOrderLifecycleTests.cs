using ClearMeasure.Bootcamp.AcceptanceTests.WorkItemTracking;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.IntegrationTests;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpWorkOrderLifecycleTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    private static McpTestHelper? _helper;

    [OneTimeSetUp]
    public async Task McpSetUp()
    {
        _helper = new McpTestHelper();
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
        var creator = employees.First(e => e.Roles.Any(r => r.CanCreateWorkOrder));
        var assignee = employees.First(e =>
            e.Roles.Any(r => r.CanFulfillWorkOrder) && e.UserName != creator.UserName);

        // Step 1: Create a draft work order
        var tracking = new McpWorkItemTrackingService(_helper!);
        var createResult = await tracking.CreateDraftWorkOrderAsync(
            "Lifecycle test work order",
            "Testing full lifecycle via direct MCP tool calls",
            creator.UserName!);

        createResult.ShouldContain("Lifecycle test work order");
        createResult.ShouldContain("Draft");
        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrEmpty("Work order number should be returned");

        // Step 2: Assign the work order (Draft -> Assigned)
        var assignResult = await tracking.ExecuteWorkOrderCommandAsync(
            workOrderNumber,
            "DraftToAssignedCommand",
            creator.UserName!,
            assignee.UserName!);

        assignResult.ShouldContain("Assigned");
        assignResult.ShouldContain(assignee.GetFullName());

        // Step 3: Begin work (Assigned -> InProgress)
        var beginResult = await tracking.ExecuteWorkOrderCommandAsync(
            workOrderNumber,
            "AssignedToInProgressCommand",
            assignee.UserName!);

        beginResult.ShouldContain("In Progress");

        // Step 4: Complete work (InProgress -> Complete)
        var completeResult = await tracking.ExecuteWorkOrderCommandAsync(
            workOrderNumber,
            "InProgressToCompleteCommand",
            assignee.UserName!);

        completeResult.ShouldContain("Complete");
        completeResult.ShouldContain("CompletedDate");

        // Step 5: Verify final state via get-work-order
        var getResult = await tracking.GetWorkOrderAsync(workOrderNumber);

        getResult.ShouldContain("Complete");
        getResult.ShouldContain("Lifecycle test work order");
        getResult.ShouldContain(creator.GetFullName());
        getResult.ShouldContain(assignee.GetFullName());
    }

}
