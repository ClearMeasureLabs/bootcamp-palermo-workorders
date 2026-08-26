using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
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
        FooterNote
    }

    /// <summary>
    /// Calendar year shown in the site copyright line (UTC, matches acceptance tests).
    /// </summary>
    protected int CopyrightYear => DateTime.UtcNow.Year;

    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    [Inject]
    private ThemePreferenceService Theme { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private ElementReference _navToggleButtonRef;
    private DotNetObjectReference<MainLayout>? _dotNetRef;
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _navToggleHelper;
    private bool _isNarrowViewport;
    private bool _viewportSynced;
    private bool _navVisible = true;

    private string AppContainerClass =>
        $"{NavRailCss.AppContainerClass(_isNarrowViewport, _navVisible)}{(IsWorkOrderSearchPage ? " b3-stack-chrome" : string.Empty)}";

    private string SidebarClass => NavRailCss.SidebarClass(_isNarrowViewport, _navVisible);

    private bool IsWorkOrderSearchPage =>
        Navigation.ToBaseRelativePath(Navigation.Uri).Split('?', '#')[0]
            .Equals("workorder/search", StringComparison.OrdinalIgnoreCase);

    private string NavToggleTitle =>
        _navVisible ? "Hide navigation panel" : "Show navigation panel";

    private string NavToggleAriaExpanded => _navVisible ? "true" : "false";

    protected override void OnInitialized()
    {
        Navigation.LocationChanged += HandleLocationChanged;
    }

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
            await InvokeAsync(StateHasChanged);
        }
        catch (JSDisconnectedException)
        {
        }
    }

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

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        InvokeAsync(StateHasChanged);
    }

    private static string UserInitial(string? userName)
    {
        return string.IsNullOrWhiteSpace(userName) ? "?" : userName[..1].ToUpperInvariant();
    }

    public async ValueTask DisposeAsync()
    {
        Navigation.LocationChanged -= HandleLocationChanged;

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
