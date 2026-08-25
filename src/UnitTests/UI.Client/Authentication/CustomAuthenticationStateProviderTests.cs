using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;

[TestFixture]
public class CustomAuthenticationStateProviderTests
{
    [Test]
    public async Task ShouldReturnUnauthenticatedUserWhenNotLoggedIn()
    {
        var store = new StubUserSessionStore();
        var authProvider = new CustomAuthenticationStateProvider(store);

        var authState = await authProvider.GetAuthenticationStateAsync();

        authState.User.Identity!.IsAuthenticated.ShouldBeFalse();
    }

    [Test]
    public async Task ShouldReturnAuthenticatedUserAfterLogin()
    {
        var store = new StubUserSessionStore();
        var authProvider = new CustomAuthenticationStateProvider(store);
        const string username = "hsimpson";

        await authProvider.Login(username);
        var authState = await authProvider.GetAuthenticationStateAsync();

        authState.User.Identity!.IsAuthenticated.ShouldBeTrue();
        authState.User.Identity.Name.ShouldBe(username);
    }

    [Test]
    public async Task ShouldReturnUnauthenticatedUserAfterLogout()
    {
        var store = new StubUserSessionStore();
        var authProvider = new CustomAuthenticationStateProvider(store);
        await authProvider.Login("hsimpson");

        await authProvider.Logout();
        var authState = await authProvider.GetAuthenticationStateAsync();

        authState.User.Identity!.IsAuthenticated.ShouldBeFalse();
    }

    [Test]
    public async Task Login_ShouldWriteStoreThenSetPrincipalThenNotify()
    {
        var store = new StubUserSessionStore();
        var authProvider = new CustomAuthenticationStateProvider(store);
        store.IsAuthenticatedSnapshot = authProvider.IsAuthenticated;
        authProvider.AuthenticationStateChanged += _ =>
            store.Operations.Add(authProvider.IsAuthenticated()
                ? "NotifyAuthenticated"
                : "NotifyUnauthenticated");

        await authProvider.Login("tlovejoy");

        store.Operations.ShouldBe(["SetBeforeAuthenticated", "NotifyAuthenticated"]);
        (await store.GetAsync()).ShouldBe("tlovejoy");
        authProvider.IsAuthenticated().ShouldBeTrue();
        authProvider.GetUsername().ShouldBe("tlovejoy");
    }

    [Test]
    public async Task Logout_ShouldClearStoreThenClearPrincipalThenNotify()
    {
        var store = new StubUserSessionStore();
        var authProvider = new CustomAuthenticationStateProvider(store);
        store.IsAuthenticatedSnapshot = authProvider.IsAuthenticated;
        await authProvider.Login("tlovejoy");
        store.Operations.Clear();
        authProvider.AuthenticationStateChanged += _ =>
            store.Operations.Add(authProvider.IsAuthenticated()
                ? "NotifyAuthenticated"
                : "NotifyUnauthenticated");

        await authProvider.Logout();

        store.Operations.ShouldBe(["ClearWhileAuthenticated", "NotifyUnauthenticated"]);
        (await store.GetAsync()).ShouldBeNull();
        authProvider.IsAuthenticated().ShouldBeFalse();
    }

    [Test]
    public async Task GetAuthenticationStateAsync_ShouldRestoreTlovejoyFromStoreWithoutLogin()
    {
        var store = new StubUserSessionStore { Username = "tlovejoy" };
        var authProvider = new CustomAuthenticationStateProvider(store);

        var authState = await authProvider.GetAuthenticationStateAsync();

        authState.User.Identity!.IsAuthenticated.ShouldBeTrue();
        authState.User.Identity.Name.ShouldBe("tlovejoy");
        authProvider.GetUsername().ShouldBe("tlovejoy");
        store.Operations.ShouldContain("Get");
    }

    [Test]
    public async Task GetAuthenticationStateAsync_ShouldStayUnauthenticated_AfterLogoutClearsStore()
    {
        var store = new StubUserSessionStore();
        var authProvider = new CustomAuthenticationStateProvider(store);
        await authProvider.Login("tlovejoy");
        await authProvider.Logout();

        var restoredProvider = new CustomAuthenticationStateProvider(store);
        var authState = await restoredProvider.GetAuthenticationStateAsync();

        authState.User.Identity!.IsAuthenticated.ShouldBeFalse();
        (await store.GetAsync()).ShouldBeNull();
    }

    [Test]
    public async Task Logout_ShouldRemainUnauthenticated_WhenRestoreWasAlreadyInFlight()
    {
        var pendingGet = new TaskCompletionSource<string?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new StubUserSessionStore { PendingGet = pendingGet };
        var authProvider = new CustomAuthenticationStateProvider(store);
        var restoreTask = authProvider.GetAuthenticationStateAsync();

        var logoutTask = authProvider.Logout();
        pendingGet.SetResult("tlovejoy");

        await restoreTask;
        await logoutTask;
        var authState = await authProvider.GetAuthenticationStateAsync();

        authState.User.Identity!.IsAuthenticated.ShouldBeFalse();
        authProvider.GetUsername().ShouldBeNull();
    }

    [Test]
    public async Task Login_ShouldRemainUnauthenticated_WhenStoreWriteFails()
    {
        var store = new StubUserSessionStore
        {
            SetException = new InvalidOperationException("Storage unavailable")
        };
        var authProvider = new CustomAuthenticationStateProvider(store);

        await Should.ThrowAsync<InvalidOperationException>(() => authProvider.Login("tlovejoy"));

        authProvider.IsAuthenticated().ShouldBeFalse();
        authProvider.GetUsername().ShouldBeNull();
    }

    [Test]
    public async Task Logout_ShouldRemainAuthenticated_WhenStoreClearFails()
    {
        var store = new StubUserSessionStore();
        var authProvider = new CustomAuthenticationStateProvider(store);
        await authProvider.Login("tlovejoy");
        store.ClearException = new InvalidOperationException("Storage unavailable");

        await Should.ThrowAsync<InvalidOperationException>(authProvider.Logout);

        authProvider.IsAuthenticated().ShouldBeTrue();
        authProvider.GetUsername().ShouldBe("tlovejoy");
        store.Username.ShouldBe("tlovejoy");
    }

    [Test]
    public async Task ClearAsync_AfterSet_ShouldYieldNullOrEmpty()
    {
        var store = new StubUserSessionStore();
        await store.SetAsync("tlovejoy");

        await store.ClearAsync();

        (await store.GetAsync()).ShouldBeNullOrEmpty();
    }

    [Test]
    public async Task ClearAsync_ShouldThrow_WhenUsernameRemainsAfterClear()
    {
        var store = new StubUserSessionStore
        {
            Username = "tlovejoy",
            KeepUsernameOnClear = true
        };

        await Should.ThrowAsync<InvalidOperationException>(store.ClearAsync);

        store.Username.ShouldBe("tlovejoy");
    }

    [Test]
    public async Task Logout_ShouldRemainAuthenticatedAndNotNotify_WhenStubbornTlovejoyStoreThrows()
    {
        var store = new StubUserSessionStore { KeepUsernameOnClear = true };
        var authProvider = new CustomAuthenticationStateProvider(store);
        await authProvider.Login("tlovejoy");
        store.Operations.Clear();
        var notified = false;
        authProvider.AuthenticationStateChanged += _ => notified = true;

        await Should.ThrowAsync<InvalidOperationException>(authProvider.Logout);

        authProvider.IsAuthenticated().ShouldBeTrue();
        authProvider.GetUsername().ShouldBe("tlovejoy");
        store.Username.ShouldBe("tlovejoy");
        notified.ShouldBeFalse();
    }

    [Test]
    public async Task Logout_ShouldEmptyStoreUnauthenticateAndNotify_WhenClearSucceeds()
    {
        var store = new StubUserSessionStore();
        var authProvider = new CustomAuthenticationStateProvider(store);
        await authProvider.Login("tlovejoy");
        store.Operations.Clear();
        var notifications = new List<bool>();
        authProvider.AuthenticationStateChanged += _ =>
            notifications.Add(authProvider.IsAuthenticated());

        await authProvider.Logout();

        (await store.GetAsync()).ShouldBeNullOrEmpty();
        authProvider.IsAuthenticated().ShouldBeFalse();
        notifications.ShouldBe([false]);
    }

    [Test]
    public async Task Login_ShouldWriteStore_WhenCalledAfterRestoreOfTlovejoy()
    {
        var store = new StubUserSessionStore { Username = "tlovejoy" };
        var authProvider = new CustomAuthenticationStateProvider(store);
        var restored = await authProvider.GetAuthenticationStateAsync();
        restored.User.Identity!.IsAuthenticated.ShouldBeTrue();
        restored.User.Identity.Name.ShouldBe("tlovejoy");

        await authProvider.Login("tlovejoy");

        (await store.GetAsync()).ShouldBe("tlovejoy");
        authProvider.IsAuthenticated().ShouldBeTrue();
        authProvider.GetUsername().ShouldBe("tlovejoy");
    }
}
