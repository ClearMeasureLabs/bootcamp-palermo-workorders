using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ClearMeasure.Bootcamp.UI.Shared.Authentication;

/// <summary>
/// In-memory authentication state with browser persistence of the selected username.
/// Sole writer of <see cref="IUserSessionStore"/>.
/// </summary>
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private readonly IUserSessionStore _userSessionStore;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    /// <summary>
    /// Creates the provider with the given session store.
    /// </summary>
    /// <param name="userSessionStore">Persisted username store; only this provider writes to it.</param>
    public CustomAuthenticationStateProvider(IUserSessionStore userSessionStore)
    {
        _userSessionStore = userSessionStore;
    }

    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return new AuthenticationState(await ResolvePrincipalAsync());
    }

    /// <summary>
    /// Persists the username, sets the authenticated principal, then notifies listeners.
    /// </summary>
    /// <param name="username">Selected employee username.</param>
    public async Task Login(string username)
    {
        await _stateLock.WaitAsync();
        try
        {
            await _userSessionStore.SetAsync(username);
            _currentUser = CreatePrincipal(username);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }
        finally
        {
            _stateLock.Release();
        }
    }

    /// <summary>
    /// Clears the persisted username, clears the principal, then notifies listeners.
    /// </summary>
    public async Task Logout()
    {
        await _stateLock.WaitAsync();
        try
        {
            await _userSessionStore.ClearAsync();
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }
        finally
        {
            _stateLock.Release();
        }
    }

    /// <summary>
    /// Returns whether the in-memory principal is authenticated.
    /// </summary>
    public bool IsAuthenticated()
    {
        return _currentUser.Identity?.IsAuthenticated ?? false;
    }

    /// <summary>
    /// Returns the in-memory principal username, if any.
    /// </summary>
    public string? GetUsername()
    {
        return _currentUser.Identity?.Name;
    }

    /// <summary>
    /// Returns the current username, restoring it from <see cref="IUserSessionStore"/> first when the
    /// in-memory principal is not authenticated yet. Prefer this over <see cref="GetUsername"/> when the
    /// caller may run before the framework has resolved the cascaded authentication state.
    /// </summary>
    public async Task<string?> GetUsernameAsync()
    {
        var principal = await ResolvePrincipalAsync();
        return principal.Identity?.Name;
    }

    /// <summary>
    /// Hydrates <see cref="_currentUser"/> from the session store on first use and returns it under the
    /// state lock. Shared by <see cref="GetAuthenticationStateAsync"/> and <see cref="GetUsernameAsync"/>
    /// so the two accessors can never disagree.
    /// </summary>
    private async Task<ClaimsPrincipal> ResolvePrincipalAsync()
    {
        await _stateLock.WaitAsync();
        try
        {
            if (!IsAuthenticated())
            {
                var username = await _userSessionStore.GetAsync();
                if (!string.IsNullOrEmpty(username))
                {
                    _currentUser = CreatePrincipal(username);
                }
            }

            return _currentUser;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private static ClaimsPrincipal CreatePrincipal(string username)
    {
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.Name, username)
        ], "Custom Authentication");
        return new ClaimsPrincipal(identity);
    }
}
