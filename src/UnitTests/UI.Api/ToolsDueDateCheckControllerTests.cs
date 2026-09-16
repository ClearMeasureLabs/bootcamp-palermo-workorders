using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsDueDateCheckControllerTests
{
    [Test]
    public void Get_Should_ReturnOverdue_When_DateIsInPast()
    {
        var result = CreateController().Get("2000-01-01");

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<DueDateCheckResponse>();
        payload.Urgency.ShouldBe("Overdue");
    }

    [Test]
    public void Get_Should_ReturnNone_When_DateIsInFuture()
    {
        var result = CreateController().Get("2099-12-31");

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<DueDateCheckResponse>();
        payload.Urgency.ShouldBe("None");
    }

    [Test]
    public void Get_Should_Return400_When_DateMissing()
    {
        var nullResult = CreateController().Get(null);
        var emptyResult = CreateController().Get("");
        var whitespaceResult = CreateController().Get("   ");

        foreach (var result in new[] { nullResult, emptyResult, whitespaceResult })
        {
            var objectResult = result.ShouldBeOfType<ObjectResult>();
            objectResult.StatusCode.ShouldBe(400);
            objectResult.Value.ShouldBeOfType<ProblemDetails>();
        }
    }

    [Test]
    public void Get_Should_Return400_When_DateInvalidFormat()
    {
        var result = CreateController().Get("not-a-date");

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    [Test]
    public void Get_Should_Return400_When_DateWrongFormat()
    {
        // MM/dd/yyyy is wrong format — must be yyyy-MM-dd
        var result = CreateController().Get("01/15/2025");

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    private static ToolsDueDateCheckController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}
