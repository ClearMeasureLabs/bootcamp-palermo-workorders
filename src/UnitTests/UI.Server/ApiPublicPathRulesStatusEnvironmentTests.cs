using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiPublicPathRulesStatusEnvironmentTests
{
    [TestCase("/api/status/environment")]
    [TestCase("/api/v1.0/status/environment")]
    public void Should_TreatStatusEnvironmentAsNonPublic_When_PathIsApiStatusEnvironment(string path)
    {
        ApiKeyAuthenticationMiddleware.IsPublicVersionOrTimePath(path).ShouldBeFalse();
    }
}
