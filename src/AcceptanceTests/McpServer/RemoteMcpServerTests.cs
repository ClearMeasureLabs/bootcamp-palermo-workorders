using ClearMeasure.Bootcamp.AcceptanceTests;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Shouldly;

namespace ClearMeasure.Bootcamp.McpAcceptanceTests;

[TestFixture]
public class RemoteMcpServerTests
{
    [Test]
    public async Task ShouldConnectToRemoteMcpEndpointAndDiscoverTools()
    {
        var baseUrl = ServerFixture.ApplicationBaseUrl;
        if (string.IsNullOrEmpty(baseUrl))
            Assert.Inconclusive("UI.Server is not available");

        await using var client = await CreateRemoteMcpClient(baseUrl);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var tools = await client.ListToolsAsync(cancellationToken: cts.Token);

        TestContext.Out.WriteLine($"Remote MCP endpoint returned {tools.Count} tools");
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
    public async Task ShouldListEmployeesViaRemoteMcpEndpoint()
    {
        var baseUrl = ServerFixture.ApplicationBaseUrl;
        if (string.IsNullOrEmpty(baseUrl))
            Assert.Inconclusive("UI.Server is not available");

        await using var client = await CreateRemoteMcpClient(baseUrl);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var result = await client.CallToolAsync("list-employees", new Dictionary<string, object?>(),
            cancellationToken: cts.Token);

        var text = string.Join("\n", result.Content
            .OfType<TextContentBlock>()
            .Select(c => c.Text));

        TestContext.Out.WriteLine($"list-employees result: {text[..Math.Min(text.Length, 500)]}");
        text.ShouldNotBeNullOrEmpty();
        text.ShouldContain("UserName");
    }

    [Test]
    public async Task ShouldListResourcesViaRemoteMcpEndpoint()
    {
        var baseUrl = ServerFixture.ApplicationBaseUrl;
        if (string.IsNullOrEmpty(baseUrl))
            Assert.Inconclusive("UI.Server is not available");

        await using var client = await CreateRemoteMcpClient(baseUrl);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resources = await client.ListResourcesAsync(cancellationToken: cts.Token);

        TestContext.Out.WriteLine($"Remote MCP endpoint returned {resources.Count} resources");
        resources.Count.ShouldBeGreaterThanOrEqualTo(3);

        var resourceNames = resources.Select(r => r.Name).ToList();
        resourceNames.ShouldContain("work-order-statuses");
        resourceNames.ShouldContain("roles");
        resourceNames.ShouldContain("status-transitions");
    }

    private static async Task<McpClient> CreateRemoteMcpClient(string baseUrl)
    {
        var endpoint = new Uri($"{baseUrl}/mcp");
        TestContext.Out.WriteLine($"Connecting to remote MCP endpoint at {endpoint}");

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = endpoint,
            Name = "ChurchBulletin-Remote"
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        return await McpClient.CreateAsync(transport, cancellationToken: cts.Token);
    }
}
