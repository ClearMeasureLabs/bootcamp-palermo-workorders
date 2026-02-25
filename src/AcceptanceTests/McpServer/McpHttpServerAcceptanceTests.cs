using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.IntegrationTests;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ModelContextProtocol.Protocol;
using Shouldly;

namespace ClearMeasure.Bootcamp.McpAcceptanceTests;

[TestFixture]
public class McpHttpServerAcceptanceTests
{
    [SetUp]
    public void EnsureAvailability()
    {
        if (!McpHttpServerFixture.ServerAvailable)
            Assert.Inconclusive("MCP HTTP server is not available");
    }

    [Test]
    public void ShouldDiscoverAllMcpToolsViaHttp()
    {
        var tools = McpHttpServerFixture.Tools!;
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
        var result = await McpHttpServerFixture.McpClientInstance!.CallToolAsync("list-work-orders",
            new Dictionary<string, object?>());

        var text = string.Join("\n", result.Content
            .OfType<TextContentBlock>()
            .Select(c => c.Text));

        text.ShouldNotBeNullOrEmpty();
        text.ShouldContain("Number");
    }

    [Test]
    public async Task ShouldCreateWorkOrderViaHttp()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var creator = employees.First(e => e.Roles.Any(r => r.CanCreateWorkOrder));

        var result = await McpHttpServerFixture.McpClientInstance!.CallToolAsync("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = "HTTP transport test",
                ["description"] = "Created via HTTP MCP transport",
                ["creatorUsername"] = creator.UserName!
            });

        var text = string.Join("\n", result.Content
            .OfType<TextContentBlock>()
            .Select(c => c.Text));

        text.ShouldContain("HTTP transport test");
        text.ShouldContain("Draft");
    }

    [Test]
    public async Task ShouldGetEmployeeViaHttp()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var known = employees.First();

        var result = await McpHttpServerFixture.McpClientInstance!.CallToolAsync("get-employee",
            new Dictionary<string, object?>
            {
                ["username"] = known.UserName!
            });

        var text = string.Join("\n", result.Content
            .OfType<TextContentBlock>()
            .Select(c => c.Text));

        text.ShouldNotBeNullOrEmpty();
        text.ShouldContain(known.UserName!);
    }
}
