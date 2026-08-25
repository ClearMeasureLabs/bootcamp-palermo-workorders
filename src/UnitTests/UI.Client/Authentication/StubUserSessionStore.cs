using ClearMeasure.Bootcamp.UI.Shared.Authentication;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;

/// <summary>
/// In-memory <see cref="IUserSessionStore"/> for unit tests.
/// </summary>
public sealed class StubUserSessionStore : IUserSessionStore
{
    private readonly Func<bool>? _isAuthenticatedSnapshot;

    /// <summary>
    /// Creates an empty in-memory store.
    /// </summary>
    public StubUserSessionStore()
    {
    }

    /// <summary>
    /// Creates a store that records whether authentication was already set when Set/Clear run.
    /// </summary>
    /// <param name="isAuthenticatedSnapshot">Callback evaluated during Set/Clear for ordering assertions.</param>
    public StubUserSessionStore(Func<bool> isAuthenticatedSnapshot)
    {
        _isAuthenticatedSnapshot = isAuthenticatedSnapshot;
    }

    /// <summary>
    /// Currently stored username, or null when cleared.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Ordered operation labels for assertions (Set/Clear/Get and auth timing markers).
    /// </summary>
    public List<string> Operations { get; } = [];

    /// <inheritdoc />
    public Task<string?> GetAsync()
    {
        Operations.Add("Get");
        return Task.FromResult(Username);
    }

    /// <inheritdoc />
    public Task SetAsync(string username)
    {
        if (_isAuthenticatedSnapshot is not null)
        {
            Operations.Add(_isAuthenticatedSnapshot() ? "SetWhileAuthenticated" : "SetBeforeAuthenticated");
        }
        else
        {
            Operations.Add("Set");
        }

        Username = username;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ClearAsync()
    {
        if (_isAuthenticatedSnapshot is not null)
        {
            Operations.Add(_isAuthenticatedSnapshot() ? "ClearWhileAuthenticated" : "ClearWhileUnauthenticated");
        }
        else
        {
            Operations.Add("Clear");
        }

        Username = null;
        return Task.CompletedTask;
    }
}
