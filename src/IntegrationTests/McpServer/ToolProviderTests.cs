using ClearMeasure.Bootcamp.McpServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class ToolProviderTests
{
    private WebApplication? _application;

    [OneTimeSetUp]
    public async Task StartMcpHttpHost()
    {
        var sqlitePath = Path.Combine(Path.GetTempPath(), $"mcp-toolprovider-{Guid.NewGuid():N}.db");
        _application = McpServerApplication.BuildApplication(["--http"], builder =>
        {
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddHttpClient();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = $"Data Source={sqlitePath}"
            });
        });

        await _application.StartAsync();
    }

    [OneTimeTearDown]
    public async Task StopMcpHttpHost()
    {
        if (_application != null)
        {
            await _application.StopAsync();
            await _application.DisposeAsync();
            _application = null;
        }
    }

    [Test]
    public async Task ShouldDiscoverToolsFromCoHostedMcpEndpoint()
    {
        var server = _application!.Services.GetRequiredService<IServer>();
        var httpClientFactory = _application.Services.GetRequiredService<IHttpClientFactory>();
        var logger = _application.Services.GetRequiredService<ILogger<ToolProvider>>();

        await using var provider = new ToolProvider(server, httpClientFactory, logger);
        var tools = await provider.GetToolsAsync();

        tools.Count.ShouldBeGreaterThanOrEqualTo(6);
        tools.Select(t => t.Name).ShouldContain("list-work-orders");
    }

    [Test]
    public async Task ShouldReturnCachedToolsOnSecondCall()
    {
        var server = _application!.Services.GetRequiredService<IServer>();
        var httpClientFactory = _application.Services.GetRequiredService<IHttpClientFactory>();
        var logger = _application.Services.GetRequiredService<ILogger<ToolProvider>>();

        await using var provider = new ToolProvider(server, httpClientFactory, logger);
        var first = await provider.GetToolsAsync();
        var second = await provider.GetToolsAsync();

        second.ShouldBeSameAs(first);
    }
}
