using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.IntegrationTests;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpServerAcceptanceTests : AcceptanceTestBase
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
    public async Task ShouldDiscoverAllMcpTools()
    {
        _helper!.Tools.Count.ShouldBeGreaterThanOrEqualTo(7);

        var toolNames = _helper.Tools.Select(t => t.Name).ToList();
        toolNames.ShouldContain("list-work-orders");
        toolNames.ShouldContain("get-work-order");
        toolNames.ShouldContain("create-work-order");
        toolNames.ShouldContain("execute-work-order-command");
        toolNames.ShouldContain("update-work-order-description");
        toolNames.ShouldContain("list-employees");
        toolNames.ShouldContain("get-employee");
    }

    [Test]
    public async Task ShouldCreateWorkOrderViaDirectToolCall()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var creator = employees.First(e => e.Roles.Any(r => r.CanCreateWorkOrder));

        var result = await _helper!.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = "Direct MCP tool test",
                ["description"] = "Created via direct tool call",
                ["creatorUsername"] = creator.UserName!
            });

        result.ShouldContain("Direct MCP tool test");
        result.ShouldContain("Draft");
    }

    [Test, Retry(2)]
    public async Task ShouldListWorkOrdersViaLlm()
    {
        var response = await _helper!.SendPrompt(
            "Use the list-work-orders tool to list all work orders in the system. " +
            "Return the work order numbers you find.");

        response.Text.ShouldNotBeNullOrEmpty();
    }

    [Test, Retry(2)]
    public async Task ShouldGetWorkOrderByNumberViaLlm()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var workOrders = await bus.Send(new WorkOrderSpecificationQuery());
        var knownOrder = workOrders.First();

        var response = await _helper!.SendPrompt(
            $"Use the get-work-order tool to get the details of work order number '{knownOrder.Number}'. " +
            "Return the title and status.");

        response.Text.ShouldNotBeNullOrEmpty();
        response.Text.ShouldContain(knownOrder.Title!);
    }

    [Test, Retry(2)]
    public async Task ShouldCreateWorkOrderViaLlm()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var creator = employees.First(e => e.Roles.Any(r => r.CanCreateWorkOrder));

        var response = await _helper!.SendPrompt(
            $"Call the create-work-order tool with these exact parameters: " +
            $"title='Repair sanctuary roof', description='Roof tiles need replacement', " +
            $"creatorUsername='{creator.UserName}'.");

        response.Text.ShouldNotBeNullOrEmpty();
        var responseText = response.Text.ToLowerInvariant();
        (responseText.Contains("repair") || responseText.Contains("draft") || responseText.Contains("created") || responseText.Contains("wo-"))
            .ShouldBeTrue($"Expected creation confirmation in response: {response.Text}");
    }

    [Test, Retry(2)]
    public async Task ShouldListEmployeesViaLlm()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var knownUsernames = employees.Select(e => e.UserName).ToList();

        var response = await _helper!.SendPrompt(
            "Use the list-employees tool to list all employees in the system. " +
            "Return their usernames.");

        response.Text.ShouldNotBeNullOrEmpty();
        knownUsernames.ShouldContain(
            username => response.Text.Contains(username!),
            "Response should contain at least one known employee username");
    }
}
