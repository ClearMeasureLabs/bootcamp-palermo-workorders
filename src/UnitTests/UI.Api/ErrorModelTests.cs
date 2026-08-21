using ClearMeasure.Bootcamp.UI.Api.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ErrorModelTests
{
    [Test]
    public void OnGet_Should_SetRequestId_FromTraceIdentifier()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-8970";
        var model = CreateModel(httpContext);

        model.OnGet();

        model.RequestId.ShouldBe("trace-8970");
        model.ShowRequestId.ShouldBeTrue();
    }

    [Test]
    public void ShowRequestId_Should_BeFalse_When_RequestIdEmpty()
    {
        var model = CreateModel(new DefaultHttpContext());
        model.RequestId = "";

        model.ShowRequestId.ShouldBeFalse();
    }

    private static ErrorModel CreateModel(HttpContext httpContext)
    {
        var model = new ErrorModel(NullLogger<ErrorModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = httpContext }
        };
        return model;
    }
}
