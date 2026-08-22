using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiKeyAuthenticationMiddlewarePublicPathTests
{
    [TestCase("/api/version", true)]
    [TestCase("/api/v1.0/version", true)]
    [TestCase("/api/time", true)]
    [TestCase("/api/v1.0/ping", true)]
    [TestCase("/api/tools/hash", true)]
    [TestCase("/api/v1.0/tools/hash", true)]
    [TestCase("/api/health", false)]
    [TestCase("/mcp", false)]
    public void IsPublicVersionOrTimePath_ReturnsExpected(string path, bool expectedPublic)
    {
        ApiKeyAuthenticationMiddleware.IsPublicVersionOrTimePath(path).ShouldBe(expectedPublic);
    }

    [Test]
    public void IsPublicVersionOrTimePath_ReturnsTrue_ForToolsHashUnversionedAndVersioned()
    {
        ApiKeyAuthenticationMiddleware.IsPublicVersionOrTimePath("/api/tools/hash").ShouldBeTrue();
        ApiKeyAuthenticationMiddleware.IsPublicVersionOrTimePath("/api/v1.0/tools/hash").ShouldBeTrue();
    }

    [TestCase("/api/version", "version")]
    [TestCase("/api/v1.0/time", "time")]
    [TestCase("/api/tools/hash", "tools/hash")]
    [TestCase("/api/v1.0/tools/hash", "tools/hash")]
    public void TryGetLeafSegment_ReturnsLeaf(string path, string expectedLeaf)
    {
        ApiPublicPathRules.TryGetLeafSegment(path, out var leaf).ShouldBeTrue();
        leaf.ShouldBe(expectedLeaf);
    }

    [Test]
    public void IsAuthorized_ReturnsTrue_WhenKeyMatches()
    {
        var request = new DefaultHttpContext().Request;
        request.Headers["X-API-Key"] = "secret";
        ApiKeyAuthenticationMiddleware.IsAuthorized(request, "secret").ShouldBeTrue();
    }

    [Test]
    public void IsAuthorized_ReturnsFalse_WhenKeyMissing()
    {
        var request = new DefaultHttpContext().Request;
        ApiKeyAuthenticationMiddleware.IsAuthorized(request, "secret").ShouldBeFalse();
    }
}
