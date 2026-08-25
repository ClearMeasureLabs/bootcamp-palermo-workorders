using System.Net.Security;
using ClearMeasure.Bootcamp.McpServer;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpLoopbackHttpClientTests
{
    [Test]
    public void ShouldCreateClientForLoopbackHttps()
    {
        using var client = McpLoopbackHttpClient.CreateForDevCertificate("https://localhost:7174/mcp");

        client.ShouldNotBeNull();
    }

    [TestCase("http://127.0.0.1:5174/mcp")]
    [TestCase("https://church.example.com/mcp")]
    public void ShouldNotCreateClientForSharedFactoryEndpoints(string mcpUrl)
    {
        McpLoopbackHttpClient.CreateForDevCertificate(mcpUrl).ShouldBeNull();
    }

    [Test]
    public void ShouldAcceptOnlyDevCertificateChainErrors()
    {
        McpLoopbackHttpClient.IsAcceptable(SslPolicyErrors.None).ShouldBeTrue();
        McpLoopbackHttpClient.IsAcceptable(SslPolicyErrors.RemoteCertificateChainErrors).ShouldBeTrue();
        McpLoopbackHttpClient.IsAcceptable(SslPolicyErrors.RemoteCertificateNameMismatch).ShouldBeFalse();
        McpLoopbackHttpClient.IsAcceptable(SslPolicyErrors.RemoteCertificateNotAvailable).ShouldBeFalse();
    }
}
