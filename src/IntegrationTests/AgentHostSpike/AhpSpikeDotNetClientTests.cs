using ClearMeasure.Bootcamp.UI.Client.AgentHostSpike;
using ClearMeasure.Bootcamp.UI.Server.AgentHostSpike;
using Microsoft.AgentHostProtocol;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.AgentHostSpike;

/// <summary>
/// Drives <see cref="AhpSpikeConnection"/> (the same class the Blazor WASM spike page uses)
/// over a real loopback WebSocket against the spike middleware hosted by Kestrel.
/// </summary>
[NonParallelizable]
[TestFixture]
public class AhpSpikeDotNetClientTests
{
    [Test]
    public async Task Should_ConnectAndReportServerIdentity_When_InitializeSucceeds()
    {
        await using var app = await StartHostAsync(5, 1, 5000);
        var projection = new AhpSpikeChatProjection();
        var logs = new List<string>();
        await using var connection = new AhpSpikeConnection(EndpointOf(app, "alice"), projection, 4096, () => { }, logs.Add);

        await connection.ConnectAsync();

        connection.IsConnected.ShouldBeTrue();
        connection.ServerUser.ShouldBe("alice");
        connection.NegotiatedVersion.ShouldBe("1.0.0");
        projection.TurnStatus.ShouldBe("idle");
        projection.SnapshotsApplied.ShouldBe(1);
        connection.ClientId.ShouldStartWith("wasm-");
    }

    [Test]
    public async Task Should_RenderAllDeltasAndReplayMissedActions_When_ConnectionDropsMidStream()
    {
        await using var app = await StartHostAsync(200, 2, 5000);
        var projection = new AhpSpikeChatProjection();
        var logs = new List<string>();
        await using var connection = new AhpSpikeConnection(EndpointOf(app, "alice"), projection, 4096, () => { }, logs.Add);

        await connection.ConnectAsync();
        await connection.StartTurnAsync();
        await WaitUntilAsync(() => projection.DeltaActionsReceived >= 20);
        connection.SimulateNetworkDrop();
        await WaitUntilAsync(() => !connection.IsConnected);
        var journal = app.Services.GetRequiredService<AhpSpikeJournal>();
        await WaitUntilAsync(() => journal.ServerSeq > projection.LastSeenServerSeq + 5);
        await connection.ReconnectAsync();
        await WaitUntilAsync(() => projection.TurnStatus == "complete");

        projection.DeltasRendered.ShouldBe(200);
        projection.DeltaActionsReceived.ShouldBe(200);
        projection.Gaps.ShouldBe(0);
        projection.ReplayedActions.ShouldBeGreaterThan(0);
        projection.LastSeenServerSeq.ShouldBe(journal.ServerSeq);
        logs.Any(line => line.Contains("replayed", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ReseedFromSnapshot_When_ReplayBufferNoLongerHoldsMissedActions()
    {
        await using var app = await StartHostAsync(100, 0, 5);
        var projection = new AhpSpikeChatProjection();
        var logs = new List<string>();
        await using var connection = new AhpSpikeConnection(EndpointOf(app, "alice"), projection, 4096, () => { }, logs.Add);

        await connection.ConnectAsync();
        connection.SimulateNetworkDrop();
        await WaitUntilAsync(() => !connection.IsConnected);
        var streamer = app.Services.GetRequiredService<AhpSpikeTurnStreamer>();
        streamer.TryStart(new ChatTurnStartedAction
        {
            TurnId = "t1",
            StartedAt = "2026-01-01T00:00:00Z",
            Message = new Message
            {
                Text = "go",
                Origin = new MessageOrigin { Kind = MessageKind.User }
            }
        }, null).ShouldBeTrue();
        await streamer.Completion;
        await connection.ReconnectAsync();

        projection.SnapshotsApplied.ShouldBe(2);
        projection.TurnStatus.ShouldBe("complete");
        projection.DeltasRendered.ShouldBe(100);
        logs.Any(line => line.Contains("fresh snapshot", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ReportDisconnected_When_DisposedAfterConnect()
    {
        await using var app = await StartHostAsync(5, 1, 5000);
        var projection = new AhpSpikeChatProjection();
        var logs = new List<string>();
        await using var connection = new AhpSpikeConnection(EndpointOf(app, "alice"), projection, 4096, () => { }, logs.Add);

        await connection.ConnectAsync();
        await connection.DisposeAsync();

        await WaitUntilAsync(() => !connection.IsConnected);
    }

    private static async Task<WebApplication> StartHostAsync(int deltaCount, int intervalMs, int replayCapacity)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        builder.Services.Configure<AhpSpikeOptions>(options =>
        {
            options.DeltaCount = deltaCount;
            options.DeltaIntervalMilliseconds = intervalMs;
            options.ReplayCapacity = replayCapacity;
        });
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<AhpSpikeJournal>();
        builder.Services.AddSingleton<AhpSpikeTurnStreamer>();
        var app = builder.Build();
        app.UseWebSockets();
        app.UseMiddleware<AhpSpikeWebSocketMiddleware>();
        await app.StartAsync();
        return app;
    }

    private static Uri EndpointOf(WebApplication app, string user)
    {
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return new Uri(address.Replace("http://", "ws://", StringComparison.Ordinal) + AhpSpikeWebSocketMiddleware.Path + "?user=" + user);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 20_000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (!condition())
        {
            (Environment.TickCount64 < deadline).ShouldBeTrue($"condition not met within {timeoutMs} ms");
            await Task.Delay(20);
        }
    }
}
