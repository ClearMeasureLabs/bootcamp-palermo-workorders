using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class WebServiceMessageValidationMiddlewareTests
{
    [TestCase("GET", "/api/v1.0/blazor-wasm-single-api", false)]
    [TestCase("POST", "/api/v1.0/blazor-wasm-single-api", true)]
    [TestCase("POST", "/api/other", false)]
    public void IsBlazorWasmSingleApiPost_ReturnsExpected(string method, string path, bool expected)
    {
        var request = new DefaultHttpContext().Request;
        request.Method = method;
        request.Path = path;
        WebServiceMessageValidationMiddleware.IsBlazorWasmSingleApiPost(request).ShouldBe(expected);
    }

    [Test]
    public void TryDeserializeMessage_ReturnsFalse_WhenJsonInvalid()
    {
        var ok = WebServiceMessageValidationMiddleware.TryDeserializeMessage("{bad", out _, out var detail);
        ok.ShouldBeFalse();
        detail.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void TryDeserializeMessage_ReturnsFalse_WhenJsonNullBody()
    {
        var ok = WebServiceMessageValidationMiddleware.TryDeserializeMessage("null", out _, out var detail);
        ok.ShouldBeFalse();
        detail.ShouldNotBeNullOrEmpty();
    }
}
