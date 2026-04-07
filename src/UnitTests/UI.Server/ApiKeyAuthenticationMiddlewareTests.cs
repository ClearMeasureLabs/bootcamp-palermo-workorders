using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiKeyAuthenticationMiddlewareTests
{
    [TestCase("/api/health", false)]
    [TestCase("/api/v1.0/health", false)]
    [TestCase("/api/version", true)]
    [TestCase("/api/v1.0/version", true)]
    [TestCase("/api/time", true)]
    [TestCase("/api/v1.0/time", true)]
    [TestCase("/api/features/flags", true)]
    [TestCase("/api/v1.0/features/flags", true)]
    [TestCase("/api/WeatherForecast", false)]
    public void ShouldValidate_ReturnsExpected_When_PathAndOptions(string path, bool expectPublicSkip)
    {
        var options = new ApiKeyAuthenticationOptions { Enabled = true, ValidationKey = "secret" };
        var shouldValidate = ApiKeyAuthenticationMiddleware.ShouldValidate(path, options);
        if (expectPublicSkip)
        {
            shouldValidate.ShouldBeFalse();
        }
        else
        {
            shouldValidate.ShouldBeTrue();
        }
    }

    [Test]
    public void ShouldValidate_ReturnsFalse_When_Disabled()
    {
        var options = new ApiKeyAuthenticationOptions { Enabled = false, ValidationKey = "x" };
        ApiKeyAuthenticationMiddleware.ShouldValidate("/api/health", options).ShouldBeFalse();
    }

    [Test]
    public void ShouldValidate_ReturnsFalse_When_KeyEmpty()
    {
        var options = new ApiKeyAuthenticationOptions { Enabled = true, ValidationKey = "" };
        ApiKeyAuthenticationMiddleware.ShouldValidate("/api/health", options).ShouldBeFalse();
    }

    [Test]
    public void ShouldValidate_ReturnsFalse_When_PathNotApi()
    {
        var options = new ApiKeyAuthenticationOptions { Enabled = true, ValidationKey = "x" };
        ApiKeyAuthenticationMiddleware.ShouldValidate("/mcp", options).ShouldBeFalse();
    }
}
