using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class WeatherForecastControllerTests
{
    [Test]
    public void Get_Should_ReturnOk_WithFiveForecastsAndEtag()
    {
        var controller = CreateController();

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("application/json");
        var forecasts = JsonSerializer.Deserialize<WeatherForecast[]>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        forecasts.ShouldNotBeNull();
        forecasts.Length.ShouldBe(5);
        controller.Response.Headers.ETag.ToString().ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void Get_Should_Return304_When_IfNoneMatchIsAny()
    {
        var controller = CreateController();
        controller.Request.Headers.IfNoneMatch = "*";

        var result = controller.Get();

        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }

    private static WeatherForecastController CreateController()
    {
        var httpContext = new DefaultHttpContext();
        return new WeatherForecastController(NullLogger<WeatherForecastController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}
