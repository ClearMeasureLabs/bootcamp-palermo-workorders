using ClearMeasure.Bootcamp.UI.Client.AgentHostSpike;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client.AgentHostSpike;

[TestFixture]
public class BrowserWebSocketProbeTests
{
    [Test]
    public void Should_ReturnFourResultsInOrder_When_Running()
    {
        var results = BrowserWebSocketProbe.Run();

        results.Select(result => result.Name).ShouldBe(
            [
                "OperatingSystem.IsBrowser()",
                "ClientWebSocket custom request header",
                "ClientWebSocket KeepAliveInterval",
                "new Thread().Start()"
            ]);
    }

    [Test]
    public void Should_ReportIsBrowserUnsupported_When_RunningOffBrowser()
    {
        var results = BrowserWebSocketProbe.Run();

        var isBrowser = results[0];

        isBrowser.Supported.ShouldBeFalse();
        isBrowser.Detail.ShouldBe("Not running in a browser");
    }

    [Test]
    public void Should_ReportOtherProbesSupported_When_RunningOffBrowser()
    {
        var results = BrowserWebSocketProbe.Run();

        results.Skip(1).ShouldAllBe(result => result.Supported);
    }
}
