using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Services;

/// <summary>
/// Holds the current light/dark theme and persists the user's choice in browser local storage.
/// </summary>
public sealed class ThemePreferenceService : IAsyncDisposable
{
    public const string ThemeJsModulePath = "./_content/ClearMeasure.Bootcamp.UI.Shared/js/theme.js";

    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private bool _isDarkMode;
    private bool _initialized;

    public ThemePreferenceService(IJSRuntime js) => _js = js;

    public event Action? OnChange;

    public bool IsDarkMode => _isDarkMode;

    /// <summary>
    /// Loads stored or system-default theme and applies it to the document (without persisting system default).
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        try
        {
            _module = await _js.InvokeAsync<IJSObjectReference>("import", ThemeJsModulePath);
            var theme = await _module.InvokeAsync<string>("getTheme");
            _isDarkMode = string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase);
            await _module.InvokeVoidAsync("syncDomFromTheme", theme);
        }
        catch (JSDisconnectedException)
        {
        }

        _initialized = true;
        OnChange?.Invoke();
    }

    /// <summary>
    /// Sets dark mode on or off, persists to local storage, and updates the document root.
    /// </summary>
    public async Task SetDarkModeAsync(bool enabled)
    {
        if (_isDarkMode == enabled && _initialized)
            return;

        _isDarkMode = enabled;

        try
        {
            _module ??= await _js.InvokeAsync<IJSObjectReference>("import", ThemeJsModulePath);
            await _module.InvokeVoidAsync("setTheme", enabled);
        }
        catch (JSDisconnectedException)
        {
        }

        _initialized = true;
        OnChange?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }
}
