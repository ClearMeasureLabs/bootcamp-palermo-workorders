using ClearMeasure.Bootcamp.McpServer;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpServerApplicationTests
{
    [Test]
    public void ShouldUseHttpTransportWhenHttpArgProvided()
    {
        var configuration = new ConfigurationBuilder().Build();
        McpServerApplication.ShouldUseHttpTransport(["--http"], configuration).ShouldBeTrue();
    }

    [Test]
    public void ShouldUseHttpTransportWhenConfigurationSaysHttp()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Transport"] = "http" })
            .Build();

        McpServerApplication.ShouldUseHttpTransport([], configuration).ShouldBeTrue();
    }

    [Test]
    public void ShouldUseStdioTransportByDefault()
    {
        var configuration = new ConfigurationBuilder().Build();
        McpServerApplication.ShouldUseHttpTransport([], configuration).ShouldBeFalse();
    }
}
