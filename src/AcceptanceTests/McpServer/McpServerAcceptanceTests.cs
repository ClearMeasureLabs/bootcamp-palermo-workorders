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
        _helper!.Tools.Count.ShouldBeGreaterThanOrEqualTo(6);

        var toolNames = _helper.Tools.Select(t => t.Name).ToList();
        toolNames.ShouldContain("list-work-requests");
        toolNames.ShouldContain("get-work-request");
        toolNames.ShouldContain("create-work-request");
        toolNames.ShouldContain("execute-work-request-command");
        toolNames.ShouldContain("list-employees");
        toolNames.ShouldContain("get-employee");
    }

    [Test]
    public async Task ShouldCreateWorkRequestViaDirectToolCall()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var creator = employees.First(e => e.Roles.Any(r => r.CanCreateWorkRequest));

        var result = await _helper!.CallToolDirectly("create-work-request",
            new Dictionary<string, object?>
            {
                ["title"] = "Direct MCP tool test",
                ["description"] = "Created via direct tool call",
                ["creatorUsername"] = creator.UserName!
            });

        result.ShouldContain("Direct MCP tool test");
        result.ShouldContain("Draft");
    }
}
