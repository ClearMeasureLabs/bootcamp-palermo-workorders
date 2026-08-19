using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ProblemDetailsStatusCodePagesExtensionsTests
{
    [Test]
    public void CreateProblemDetails_SetsStatusAndTitle()
    {
        var details = ProblemDetailsStatusCodePagesExtensions.CreateProblemDetails(StatusCodes.Status404NotFound);
        details.Status.ShouldBe(404);
        details.Title.ShouldNotBeNullOrEmpty();
        details.Type.ShouldNotBeNull().ShouldContain("rfc7231");
    }

    [TestCase(StatusCodes.Status404NotFound, "6.5.4")]
    [TestCase(StatusCodes.Status400BadRequest, "6.5.1")]
    [TestCase(StatusCodes.Status500InternalServerError, "6.6.1")]
    public void ResolveProblemDetailsType_ReturnsExpected(int statusCode, string fragment)
    {
        ProblemDetailsStatusCodePagesExtensions.ResolveProblemDetailsType(statusCode)
            .ShouldContain(fragment);
    }

    [TestCase("/api/health", true)]
    [TestCase("/mcp", true)]
    [TestCase("/", false)]
    public void IsMachineOriented_ReturnsExpected(string path, bool expected)
    {
        ProblemDetailsPaths.IsMachineOriented(path).ShouldBe(expected);
    }
}
