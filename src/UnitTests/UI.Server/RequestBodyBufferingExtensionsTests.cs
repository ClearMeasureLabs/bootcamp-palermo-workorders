using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestBodyBufferingExtensionsTests
{
    [TestCase("POST", 10L, true)]
    [TestCase("PUT", null, true)]
    [TestCase("PATCH", 5L, true)]
    [TestCase("GET", 10L, false)]
    [TestCase("POST", 0L, false)]
    public void ShouldBuffer_ReturnsExpected(string method, long? contentLength, bool expected)
    {
        var request = new DefaultHttpContext().Request;
        request.Method = method;
        if (contentLength.HasValue)
        {
            request.ContentLength = contentLength.Value;
        }

        RequestBodyBufferingExtensions.ShouldBuffer(request).ShouldBe(expected);
    }
}
