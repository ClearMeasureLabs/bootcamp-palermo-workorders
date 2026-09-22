using System.Reflection;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared;

public partial class MainLayout : IAsyncDisposable
{
    /// <summary>
    /// Must stay aligned with <c>@media (max-width: 768px)</c> in <c>MainLayout.razor.css</c> and the
    /// <c>matchMedia</c> argument in <c>mainLayoutNav.js</c>.
    /// </summary>
    public const string NavRailBreakpointMediaQuery = "(max-width: 768px)";

    public enum Elements
    {
        NavRailToggle,
        CopyrightFooter,
        FooterNote,
        SoftwareVersion,
        DarkModeToggle,
        GitSha,
        EnvironmentName,
        OpenWorkOrderCountBadge
    }

    /// <summary>
    /// Calendar year shown in the site copyright line (UTC, matches acceptance tests).
    /// </summary>
    protected int CopyrightYear => DateTime.UtcNow.Year;

    /// <summary>
    /// Informational version of the running entry assembly, e.g. "1.2.3+abc1234".
    /// </summary>
    protected string AppVersion =>
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Inject]
    private ThemePreferenceService Theme { get; set; } = null!;

    [Inject]
    private HttpClient Http { get; set; } = null!;

    [Inject]
    private IBus ApplicationBus { get; set; } = null!;

    [Inject]
    private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private ElementReference _navToggleButtonRef;
    private DotNetObjectReference<MainLayout>? _dotNetRef;
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _navToggleHelper;
    private bool _isNarrowViewport;
    private bool _viewportSynced;
    private bool _navVisible = true;
    private string _gitSha = "unknown";
    private string _environmentName = "unknown";
    private int? _openWorkOrderCount;

    private string AppContainerClass => NavRailCss.AppContainerClass(_isNarrowViewport, _navVisible);

    private string SidebarClass => NavRailCss.SidebarClass(_isNarrowViewport, _navVisible);

    private string NavToggleTitle =>
        _navVisible ? "Hide navigation panel" : "Show navigation panel";

    private string NavToggleAriaExpanded => _navVisible ? "true" : "false";

    private string DarkModeToggleTitle => Theme.IsDarkMode ? "Switch to light mode" : "Switch to dark mode";

    [JSInvokable]
    public Task OnViewportChanged(bool isNarrow)
    {
        if (!_viewportSynced)
        {
            _viewportSynced = true;
            if (isNarrow)
                _navVisible = false;
        }

        _isNarrowViewport = isNarrow;
        StateHasChanged();
        return Task.CompletedTask;
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var response = await Http.GetAsync("/api/status/environment");
            if (response.IsSuccessStatusCode)
            {
                (_gitSha, _environmentName) = await ParseEnvironmentStatusAsync(response);
            }
        }
        catch (HttpRequestException)
        {
            // graceful fallback — fields remain "unknown"
        }
        catch (JsonException)
        {
            // graceful fallback — malformed response, fields remain "unknown"
        }

        await LoadOpenWorkOrderCountIfAuthenticatedAsync();
    }

    private async Task LoadOpenWorkOrderCountIfAuthenticatedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated != true)
            return;

        _openWorkOrderCount = await ApplicationBus.Send(new OpenWorkOrderCountQuery());
    }

    /// <summary>
    /// Extracted from <see cref="OnInitializedAsync"/> to keep that method's cyclomatic
    /// complexity (and therefore its CRAP score) low; the property-presence branching lives
    /// here instead, fully exercised by <c>MainLayoutTests</c>.
    /// </summary>
    private static async Task<(string GitSha, string EnvironmentName)> ParseEnvironmentStatusAsync(
        HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var gitSha = doc.RootElement.TryGetProperty("gitSha", out var sha) ? sha.GetString() ?? "unknown" : "unknown";
        var environmentName = doc.RootElement.TryGetProperty("environmentName", out var env)
            ? env.GetString() ?? "unknown"
            : "unknown";
        return (gitSha, environmentName);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            _jsModule = await Js.InvokeAsync<IJSObjectReference>("import",
                "./_content/ClearMeasure.Bootcamp.UI.Shared/js/mainLayoutNav.js");
            _navToggleHelper = await _jsModule.InvokeAsync<IJSObjectReference>("initNavToggle", _dotNetRef,
                NavRailBreakpointMediaQuery);
        }
        catch (JSDisconnectedException)
        {
        }

        try
        {
            await Theme.InitializeAsync();
            Theme.OnChange += OnThemeChanged;
            await InvokeAsync(StateHasChanged);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private void OnThemeChanged() => InvokeAsync(StateHasChanged);

    private async Task ToggleDarkModeAsync() => await Theme.SetDarkModeAsync(!Theme.IsDarkMode);

    private async Task ToggleNavRailAsync()
    {
        var wasVisible = _navVisible;
        _navVisible = !wasVisible;
        await InvokeAsync(StateHasChanged);

        if (_isNarrowViewport && wasVisible)
        {
            await Task.Yield();
            try
            {
                await _navToggleButtonRef.FocusAsync();
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        Theme.OnChange -= OnThemeChanged;

        if (_navToggleHelper is not null)
        {
            try
            {
                await _navToggleHelper.InvokeVoidAsync("dispose");
            }
            catch (JSDisconnectedException)
            {
            }

            await _navToggleHelper.DisposeAsync();
        }

        if (_jsModule is not null)
            await _jsModule.DisposeAsync();

        _dotNetRef?.Dispose();
    }
}

