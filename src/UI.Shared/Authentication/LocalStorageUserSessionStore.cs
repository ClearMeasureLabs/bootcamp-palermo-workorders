using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Authentication;

/// <summary>
/// WASM <see cref="IUserSessionStore"/> that reads and writes browser localStorage via <see cref="IJSRuntime"/>.
/// </summary>
public sealed class LocalStorageUserSessionStore : IUserSessionStore
{
    /// <summary>
    /// Browser localStorage key for the selected username.
    /// </summary>
    public const string StorageKey = "bootcamp.userSession.username";

    private readonly IJSRuntime _js;

    /// <summary>
    /// Creates a store backed by browser localStorage.
    /// </summary>
    /// <param name="js">JavaScript runtime used to access localStorage.</param>
    public LocalStorageUserSessionStore(IJSRuntime js) => _js = js;

    /// <inheritdoc />
    public async Task<string?> GetAsync()
    {
        try
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync(string username)
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, username);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    /// <inheritdoc />
    public async Task ClearAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
