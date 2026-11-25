using System.Diagnostics.CodeAnalysis;
using ClearMeasure.Bootcamp.UI.Client.AgentHostSpike;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace ClearMeasure.Bootcamp.UI.Client.Pages;

/// <summary>
/// Throwaway spike page proving the Microsoft Agent Host Protocol .NET client
/// (<see cref="AhpSpikeConnection"/>) runs inside Blazor WebAssembly.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Spike page; exercised by the AhpSpike acceptance test in a real browser")]
public partial class AhpSpike : IAsyncDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    /// <summary>Optional subscription buffer capacity override via the <c>buffer</c> query string.</summary>
    [SupplyParameterFromQuery(Name = "buffer")] public int? Buffer { get; set; }

    /// <summary>DOM test ids used by the spike acceptance test.</summary>
    public enum Elements
    {
        /// <summary>Connect button.</summary>
        ConnectButton,

        /// <summary>Start-turn button.</summary>
        StartButton,

        /// <summary>Network-drop button.</summary>
        DropButton,

        /// <summary>Reconnect button.</summary>
        ReconnectButton,

        /// <summary>Connection status cell.</summary>
        ConnectionStatus,

        /// <summary>Cell showing the user the server saw.</summary>
        ServerUser,

        /// <summary>Cell showing the negotiated protocol version.</summary>
        ProtocolVersion,

        /// <summary>Cell showing received chat/delta action count.</summary>
        DeltaActionsReceived,

        /// <summary>Cell showing deltas rendered from the reduced state.</summary>
        DeltasRendered,

        /// <summary>Cell showing the last applied serverSeq.</summary>
        LastServerSeq,

        /// <summary>Cell showing observed sequence gaps.</summary>
        Gaps,

        /// <summary>Cell showing dropped duplicates.</summary>
        Duplicates,

        /// <summary>Cell showing replayed actions on reconnect.</summary>
        Replayed,

        /// <summary>Cell showing the turn status.</summary>
        TurnStatus,

        /// <summary>Reduced chat text block.</summary>
        RenderedText,

        /// <summary>Log list.</summary>
        Log,

        /// <summary>WASM constraint probes table.</summary>
        Probes
    }

    private readonly List<string> _logLines = new();
    private readonly List<BrowserWebSocketProbe.ProbeResult> _probes = new();
    private readonly CancellationTokenSource _renderLoop = new();
    private AhpSpikeChatProjection? _projection;
    private AhpSpikeConnection? _connection;
    private bool _everConnected;
    private bool _dirty;

    /// <summary>Whether the connection is currently connected (for the status cell).</summary>
    private string ConnectionText => _connection?.IsConnected == true ? "Connected" : "Disconnected";

    protected override void OnInitialized()
    {
        _probes.AddRange(BrowserWebSocketProbe.Run());
        _ = RenderLoopAsync();
    }

    private async Task HandleConnectAsync()
    {
        try
        {
            var user = (await AuthStateProvider.GetAuthenticationStateAsync()).User.Identity?.Name ?? "anonymous";
            var baseUri = new Uri(Navigation.BaseUri);
            var scheme = baseUri.Scheme == "https" ? "wss" : "ws";
            var endpoint = new Uri($"{scheme}://{baseUri.Authority}/ws/agent-host-spike?user={Uri.EscapeDataString(user)}");
            _projection = new AhpSpikeChatProjection();
            _connection = new AhpSpikeConnection(endpoint, _projection, Buffer ?? 4096, MarkDirty, AddLog);
            await _connection.ConnectAsync();
            _everConnected = true;
        }
        catch (Exception ex)
        {
            AddLog($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task HandleStartAsync()
    {
        try
        {
            await _connection!.StartTurnAsync();
            AddLog($"turn started at {DateTime.UtcNow:O}");
        }
        catch (Exception ex)
        {
            AddLog($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private void HandleDrop()
    {
        try
        {
            _connection?.SimulateNetworkDrop();
        }
        catch (Exception ex)
        {
            AddLog($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task HandleReconnectAsync()
    {
        try
        {
            await _connection!.ReconnectAsync();
        }
        catch (Exception ex)
        {
            AddLog($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>Cancels the render loop and disposes the connection.</summary>
    public async ValueTask DisposeAsync()
    {
        await _renderLoop.CancelAsync();
        _renderLoop.Dispose();
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    private async Task RenderLoopAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        try
        {
            while (await timer.WaitForNextTickAsync(_renderLoop.Token))
            {
                if (!_dirty)
                {
                    continue;
                }

                _dirty = false;
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            _dirty = false;
        }
    }

    private void AddLog(string message)
    {
        _logLines.Insert(0, $"{DateTime.Now:HH:mm:ss.fff} {message}");
        if (_logLines.Count > 50)
        {
            _logLines.RemoveRange(50, _logLines.Count - 50);
        }

        MarkDirty();
    }

    private void MarkDirty()
    {
        _dirty = true;
    }
}
