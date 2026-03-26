using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared;

public partial class MainLayout : IAsyncDisposable
{
    public enum Elements
    {
        NavRailToggle
    }

    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    private ElementReference _navToggleButtonRef;
    private DotNetObjectReference<MainLayout>? _dotNetRef;
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _navToggleHelper;
    private bool _isNarrowViewport;
    private bool _viewportSynced;
    private bool _navVisible = true;

    private string AppContainerClass =>
        !_isNarrowViewport && !_navVisible ? "modern-app rail-collapsed" : "modern-app";

    private string SidebarClass
    {
        get
        {
            const string baseClass = "modern-sidebar";
            if (_isNarrowViewport)
                return _navVisible ? $"{baseClass} open" : baseClass;
            return _navVisible ? baseClass : $"{baseClass} rail-hidden";
        }
    }

    private string NavToggleTitle =>
        _navVisible ? "Hide navigation panel" : "Show navigation panel";

    private string NavToggleAriaExpanded => _navVisible ? "true" : "false";

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
                "(max-width: 768px)");
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

    public async ValueTask DisposeAsync()
    {
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
