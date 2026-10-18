using ClearMeasure.Bootcamp.McpServer;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class McpEndpointResolverTests
{
    [Test]
    public void BuildMcpUrl_AddressWithoutTrailingSlash_AppendsMcp()
    {
        McpEndpointResolver.BuildMcpUrl("http://127.0.0.1:5000")
            .ShouldBe("http://127.0.0.1:5000/mcp");
    }

    [Test]
    public void BuildMcpUrl_AddressWithTrailingSlash_AppendsMcp()
    {
        McpEndpointResolver.BuildMcpUrl("http://127.0.0.1:5000/")
            .ShouldBe("http://127.0.0.1:5000/mcp");
    }
}
