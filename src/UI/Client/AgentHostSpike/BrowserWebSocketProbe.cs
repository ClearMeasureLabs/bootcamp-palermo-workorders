#pragma warning disable CA1416 // these calls are deliberately probed on browser-wasm
using System.Net.WebSockets;

namespace ClearMeasure.Bootcamp.UI.Client.AgentHostSpike;

/// <summary>
/// Records the browser-WASM constraints the AHP spike must document by attempting the
/// operations the full .NET <see cref="ClientWebSocket"/> surface would use.
/// </summary>
public static class BrowserWebSocketProbe
{
    /// <summary>Outcome of a single probe.</summary>
    /// <param name="Name">What was probed.</param>
    /// <param name="Supported">Whether the operation is available on browser-wasm.</param>
    /// <param name="Detail">Human-readable explanation.</param>
    public sealed record ProbeResult(string Name, bool Supported, string Detail);

    /// <summary>Runs every probe and returns the results in a fixed order.</summary>
    public static IReadOnlyList<ProbeResult> Run() =>
        [
            ProbeIsBrowser(),
            ProbeCustomHeader(),
            ProbeKeepAlive(),
            ProbeThreadStart()
        ];

    private static ProbeResult ProbeIsBrowser()
    {
        var isBrowser = OperatingSystem.IsBrowser();
        return new ProbeResult(
            "OperatingSystem.IsBrowser()",
            isBrowser,
            isBrowser ? "Running on browser-wasm" : "Not running in a browser");
    }

    private static ProbeResult ProbeCustomHeader()
    {
        try
        {
            using var ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("X-Ahp-User", "probe");
            return new ProbeResult("ClientWebSocket custom request header", true, "Custom headers allowed");
        }
        catch (PlatformNotSupportedException ex)
        {
            return new ProbeResult(
                "ClientWebSocket custom request header",
                false,
                $"{ex.GetType().Name}: identity must travel in the query string");
        }
    }

    private static ProbeResult ProbeKeepAlive()
    {
        try
        {
            using var ws = new ClientWebSocket();
            ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            return new ProbeResult("ClientWebSocket KeepAliveInterval", true, "Keep-alive supported");
        }
        catch (PlatformNotSupportedException ex)
        {
            return new ProbeResult(
                "ClientWebSocket KeepAliveInterval",
                false,
                $"{ex.GetType().Name}: use the protocol-level AHP ping instead");
        }
    }

    private static ProbeResult ProbeThreadStart()
    {
        try
        {
            var t = new Thread(() => { });
            t.Start();
            t.Join();
            return new ProbeResult("new Thread().Start()", true, "Dedicated threads available");
        }
        catch (PlatformNotSupportedException ex)
        {
            return new ProbeResult(
                "new Thread().Start()",
                false,
                $"{ex.GetType().Name}: single-threaded runtime; AhpClient's Task.Run loops run cooperatively on the UI thread");
        }
    }
}
#pragma warning restore CA1416
