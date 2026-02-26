using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.IntegrationTests;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpHttpServerAcceptanceTests : AcceptanceTestBase
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
            Assert.Inconclusive("MCP HTTP server is not available");
    }

    [Test]
    public void ShouldDiscoverAllMcpToolsViaHttp()
    {
        var tools = _helper!.Tools;
        tools.Count.ShouldBeGreaterThanOrEqualTo(7);

        var toolNames = tools.Select(t => t.Name).ToList();
        toolNames.ShouldContain("list-work-orders");
        toolNames.ShouldContain("get-work-order");
        toolNames.ShouldContain("create-work-order");
        toolNames.ShouldContain("execute-work-order-command");
        toolNames.ShouldContain("update-work-order-description");
        toolNames.ShouldContain("list-employees");
        toolNames.ShouldContain("get-employee");
    }

    [Test]
    public async Task ShouldListWorkOrdersViaHttp()
    {
        var text = await _helper!.CallToolDirectly("list-work-orders",
            new Dictionary<string, object?>());

        text.ShouldNotBeNullOrEmpty();
        text.ShouldContain("Number");
    }

    [Test]
    public async Task ShouldCreateWorkOrderViaHttp()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var creator = employees.First(e => e.Roles.Any(r => r.CanCreateWorkOrder));

        var text = await _helper!.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = "HTTP transport test",
                ["description"] = "Created via HTTP MCP transport",
                ["creatorUsername"] = creator.UserName!
            });

        text.ShouldContain("HTTP transport test");
        text.ShouldContain("Draft");
    }

    [Test]
    public async Task ShouldGetEmployeeViaHttp()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var known = employees.First();

        var text = await _helper!.CallToolDirectly("get-employee",
            new Dictionary<string, object?>
            {
                ["username"] = known.UserName!
            });

        text.ShouldNotBeNullOrEmpty();
        text.ShouldContain(known.UserName!);
    }
}
