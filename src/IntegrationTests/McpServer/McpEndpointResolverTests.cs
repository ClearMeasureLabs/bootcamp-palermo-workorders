using ClearMeasure.Bootcamp.McpServer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpEndpointResolverTests
{
    [Test]
    public void ShouldResolveMcpUrlFromServerAddress()
    {
        var server = new StubServer(["http://127.0.0.1:5150/"]);

        McpEndpointResolver.ResolveMcpUrl(server).ShouldBe("http://127.0.0.1:5150/mcp");
    }

    [Test]
    public void ShouldPreferHttpAddressWhenHttpsAlsoPresent()
    {
        var server = new StubServer(["https://localhost:7174", "http://127.0.0.1:5174"]);

        McpEndpointResolver.ResolveMcpUrl(server).ShouldBe("http://127.0.0.1:5174/mcp");
    }

    [Test]
    public void ShouldResolveHttpsWhenOnlyHttpsIsPresent()
    {
        var server = new StubServer(["https://localhost:7174/"]);

        McpEndpointResolver.ResolveMcpUrl(server).ShouldBe("https://localhost:7174/mcp");
    }

    [Test]
    public void ShouldThrowWhenServerHasNoAddress()
    {
        var server = new StubServer([]);

        Should.Throw<InvalidOperationException>(() => McpEndpointResolver.ResolveMcpUrl(server))
            .Message.ShouldContain("Cannot determine server address");
    }

    private sealed class StubServer : IServer
    {
        public StubServer(IReadOnlyList<string> addresses)
        {
            Features = new FeatureCollection();
            var addressFeature = new ServerAddressesFeature();
            foreach (var address in addresses)
            {
                addressFeature.Addresses.Add(address);
            }

            Features.Set<IServerAddressesFeature>(addressFeature);
        }

        public IFeatureCollection Features { get; }

        public void Dispose()
        {
        }

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
            where TContext : notnull =>
            Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
