using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
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
        CustomAuthenticationStateProvider? authProvider = null;
        var store = new StubUserSessionStore(() => authProvider!.IsAuthenticated());
        authProvider = new CustomAuthenticationStateProvider(store);
        var notified = false;
        AuthenticationState? notifiedState = null;
        authProvider.AuthenticationStateChanged += async task =>
        {
            notifiedState = await task;
            notified = true;
        };

        await authProvider.Login("tlovejoy");

        store.Operations.ShouldContain("SetBeforeAuthenticated");
        store.Operations.IndexOf("SetBeforeAuthenticated").ShouldBeLessThan(
            store.Operations.Count); // set recorded during Login before notify completes
        (await store.GetAsync()).ShouldBe("tlovejoy");
        authProvider.IsAuthenticated().ShouldBeTrue();
        authProvider.GetUsername().ShouldBe("tlovejoy");
        notified.ShouldBeTrue();
        notifiedState!.User.Identity!.IsAuthenticated.ShouldBeTrue();
        notifiedState.User.Identity.Name.ShouldBe("tlovejoy");
        store.Operations[0].ShouldBe("SetBeforeAuthenticated");
    }

    [Test]
    public async Task Logout_ShouldClearStoreThenClearPrincipalThenNotify()
    {
        CustomAuthenticationStateProvider? authProvider = null;
        var store = new StubUserSessionStore(() => authProvider!.IsAuthenticated());
        authProvider = new CustomAuthenticationStateProvider(store);
        await authProvider.Login("tlovejoy");
        store.Operations.Clear();

        var notified = false;
        AuthenticationState? notifiedState = null;
        authProvider.AuthenticationStateChanged += async task =>
        {
            notifiedState = await task;
            notified = true;
        };

        await authProvider.Logout();

        store.Operations[0].ShouldBe("ClearWhileAuthenticated");
        (await store.GetAsync()).ShouldBeNull();
        authProvider.IsAuthenticated().ShouldBeFalse();
        notified.ShouldBeTrue();
        notifiedState!.User.Identity!.IsAuthenticated.ShouldBeFalse();
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
}
