using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsBusinessDaysControllerTests
{
    [Test]
    public void Get_Should_CountWeekdays_When_NormalWeekSpan()
    {
        // Monday 2025-01-06 through Friday 2025-01-10
        var result = CreateController().Get("2025-01-06", "2025-01-10");

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<BusinessDaysResponse>();
        payload.BusinessDays.ShouldBe(5);
    }

    [Test]
    public void Get_Should_Return400ProblemDetails_When_FromAfterTo()
    {
        var result = CreateController().Get("2025-01-10", "2025-01-06");

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    [Test]
    public void Get_Should_Return400ProblemDetails_When_FromMissing()
    {
        var result = CreateController().Get(null, "2025-01-10");

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    [Test]
    public void Get_Should_Return400ProblemDetails_When_ToInvalid()
    {
        var result = CreateController().Get("2025-01-06", "not-a-date");

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    [Test]
    public void CountBusinessDays_Should_ExcludeWeekends_When_SpanIncludesSaturdaySunday()
    {
        // Monday through Sunday = 5 business days
        var result = CreateController().Get("2025-01-06", "2025-01-12");

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<BusinessDaysResponse>();
        payload.BusinessDays.ShouldBe(5);
    }

    private static ToolsBusinessDaysController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}
