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
}
