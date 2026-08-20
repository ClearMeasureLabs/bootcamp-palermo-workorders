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
    [TestCase("/api/health", true)]
    [TestCase("/api/health/detailed", true)]
    [TestCase("/api/v1.0/health", true)]
    [TestCase("/api/v1.0/health/detailed", true)]
    [TestCase("/mcp", false)]
    [TestCase("/api/workorders", false)]
    public void IsPublicVersionOrTimePath_ReturnsExpected(string path, bool expectedPublic)
    {
        ApiKeyAuthenticationMiddleware.IsPublicVersionOrTimePath(path).ShouldBe(expectedPublic);
    }

    [TestCase("/api/version", "version")]
    [TestCase("/api/v1.0/time", "time")]
    [TestCase("/api/health", "health")]
    [TestCase("/api/health/detailed", "health")]
    [TestCase("/api/v1.0/health/detailed", "health")]
    public void TryGetLeafSegment_ReturnsLeaf(string path, string expectedLeaf)
    {
        ApiPublicPathRules.TryGetLeafSegment(path, out var leaf).ShouldBeTrue();
        leaf.ShouldBe(expectedLeaf);
    }

    [Test]
    public void ApiPublicPathRules_Should_TreatHealthAndDetailedAsPublic()
    {
        string[] publicHealthPaths =
        [
            "/api/health",
            "/api/health/detailed",
            "/api/v1.0/health",
            "/api/v1.0/health/detailed"
        ];

        foreach (var path in publicHealthPaths)
        {
            ApiPublicPathRules.TryGetLeafSegment(path, out var leaf).ShouldBeTrue(path);
            leaf.ShouldBe("health");
            ApiPublicPathRules.IsPublicLeaf(leaf).ShouldBeTrue(path);
            ApiKeyAuthenticationMiddleware.IsPublicVersionOrTimePath(path).ShouldBeTrue(path);
        }
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
