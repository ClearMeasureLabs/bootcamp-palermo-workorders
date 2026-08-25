namespace ClearMeasure.Bootcamp.UI.Shared.Authentication;

/// <summary>
/// Persists the selected login username for the Blazor WASM session across hard navigations.
/// </summary>
public interface IUserSessionStore
{
    /// <summary>
    /// Returns the stored username, or null when none is persisted.
    /// </summary>
    Task<string?> GetAsync();

    /// <summary>
    /// Persists the selected username.
    /// </summary>
    /// <param name="username">The username to store.</param>
    Task SetAsync(string username);

    /// <summary>
    /// Removes any persisted username.
    /// </summary>
    Task ClearAsync();
}
