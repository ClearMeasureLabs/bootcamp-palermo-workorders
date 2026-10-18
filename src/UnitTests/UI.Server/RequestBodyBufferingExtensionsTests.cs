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

        RequestBodyBufferingRules.ShouldBuffer(request).ShouldBe(expected);
    }

    [TestCase(0L, 1L)]
    [TestCase(5000L, 5000L)]
    [TestCase(50_000_000L, 10_485_760L)]
    public void ClampBufferThreshold_ReturnsExpected(long input, long expected)
    {
        RequestBodyBufferingRules.ClampBufferThreshold(input).ShouldBe(expected);
    }
}
