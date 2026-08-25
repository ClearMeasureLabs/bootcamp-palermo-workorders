using ClearMeasure.Bootcamp.UI.Shared.Authentication;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;

/// <summary>
/// In-memory <see cref="IUserSessionStore"/> for unit tests.
/// </summary>
public sealed class StubUserSessionStore : IUserSessionStore
{
    /// <summary>
    /// Currently stored username, or null when cleared.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Returns the provider's authentication state when Set/Clear run.
    /// </summary>
    public Func<bool>? IsAuthenticatedSnapshot { private get; set; }

    /// <summary>
    /// Ordered operation labels for assertions (Set/Clear/Get and auth timing markers).
    /// </summary>
    public List<string> Operations { get; } = [];

    /// <summary>
    /// Optional pending read used to control restoration timing.
    /// </summary>
    public TaskCompletionSource<string?>? PendingGet { private get; set; }

    /// <summary>
    /// Optional failure raised by <see cref="SetAsync"/>.
    /// </summary>
    public Exception? SetException { private get; init; }

    /// <summary>
    /// Optional failure raised by <see cref="ClearAsync"/>.
    /// </summary>
    public Exception? ClearException { private get; set; }

    /// <summary>
    /// When true, <see cref="ClearAsync"/> leaves the username in place and throws
    /// (simulates a stubborn localStorage key that survives removeItem).
    /// </summary>
    public bool KeepUsernameOnClear { get; set; }

    /// <inheritdoc />
    public Task<string?> GetAsync()
    {
        Operations.Add("Get");
        return PendingGet?.Task ?? Task.FromResult(Username);
    }

    /// <inheritdoc />
    public Task SetAsync(string username)
    {
        if (SetException is not null)
        {
            throw SetException;
        }

        if (IsAuthenticatedSnapshot is not null)
        {
            Operations.Add(IsAuthenticatedSnapshot() ? "SetWhileAuthenticated" : "SetBeforeAuthenticated");
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
        if (ClearException is not null)
        {
            throw ClearException;
        }

        if (IsAuthenticatedSnapshot is not null)
        {
            Operations.Add(IsAuthenticatedSnapshot() ? "ClearWhileAuthenticated" : "ClearWhileUnauthenticated");
        }
        else
        {
            Operations.Add("Clear");
        }

        if (KeepUsernameOnClear)
        {
            Operations.Add("Get");
            if (!string.IsNullOrEmpty(Username))
            {
                throw new InvalidOperationException(
                    "Failed to clear user session; username still remains after clear.");
            }
        }

        Username = null;
        PendingGet = null;
        return Task.CompletedTask;
    }
}
