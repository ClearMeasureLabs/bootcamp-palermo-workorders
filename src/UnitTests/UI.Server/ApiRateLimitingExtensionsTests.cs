using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiRateLimitingExtensionsTests
{
    [TestCase("/api/health", true)]
    [TestCase("/api/v1.0/health", true)]
    [TestCase("/api", true)]
    [TestCase("/api/v1.0/blazor-wasm-single-api", true)]
    [TestCase("/mcp", false)]
    [TestCase("/mcp/tools", false)]
    [TestCase("/health", false)]
    public void ShouldApplyToPath_ReturnsExpected_When_Path(string path, bool expected)
    {
        ApiRateLimitingExtensions.ShouldApplyToPath(new PathString(path)).ShouldBe(expected);
    }
}
