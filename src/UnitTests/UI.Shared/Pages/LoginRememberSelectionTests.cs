using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Palermo.BlazorMvc;
using Shouldly;
using Bunit;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class LoginRememberSelectionTests
{
    [Test]
    public async Task Should_SaveUsernameToLocalStorage_WhenRememberCheckedAtSignIn()
    {
        await using var ctx = CreateContext();
        ctx.JSInterop.Setup<string?>("localStorage.getItem", _ => true).SetResult(null);
        var setItem = ctx.JSInterop.SetupVoid("localStorage.setItem", _ => true);

        var cut = ctx.Render<Login>();
        await cut.Find($"[data-testid='{nameof(Login.Elements.User)}']").ChangeAsync("hsimpson");
        await cut.Find($"[data-testid='{nameof(Login.Elements.RememberMySelection)}']").ChangeAsync(true);
        await cut.Find($"[data-testid='{nameof(Login.Elements.LoginButton)}']").ClickAsync();

        setItem.VerifyInvoke("localStorage.setItem");
        var invocation = ctx.JSInterop.Invocations["localStorage.setItem"].Single();
        invocation.Arguments[0].ShouldBe(Login.RememberedUsernameStorageKey);
        invocation.Arguments[1].ShouldBe("hsimpson");
    }

    [Test]
    public async Task Should_ClearUsernameFromLocalStorage_WhenRememberUncheckedAtSignIn()
    {
        await using var ctx = CreateContext();
        ctx.JSInterop.Setup<string?>("localStorage.getItem", _ => true).SetResult(null);
        var removeItem = ctx.JSInterop.SetupVoid("localStorage.removeItem", _ => true);

        var cut = ctx.Render<Login>();
        await cut.Find($"[data-testid='{nameof(Login.Elements.User)}']").ChangeAsync("hsimpson");
        await cut.Find($"[data-testid='{nameof(Login.Elements.LoginButton)}']").ClickAsync();

        removeItem.VerifyInvoke("localStorage.removeItem");
        var invocation = ctx.JSInterop.Invocations["localStorage.removeItem"].Single();
        invocation.Arguments[0].ShouldBe(Login.RememberedUsernameStorageKey);
    }

    [Test]
    public async Task Should_PreselectSavedUsername_AndCheckRemember_WhenUsernameIsInEmployeeList()
    {
        await using var ctx = CreateContext();
        ctx.JSInterop.Setup<string?>("localStorage.getItem", _ => true).SetResult("hsimpson");

        var cut = ctx.Render<Login>();
        await cut.WaitForAssertionAsync(() =>
        {
            cut.Instance.LoginModelValue.Username.ShouldBe("hsimpson");
            cut.Find($"[data-testid='{nameof(Login.Elements.RememberMySelection)}']")
                .HasAttribute("checked")
                .ShouldBeTrue();
        });
    }

    [Test]
    public async Task Should_IgnoreSavedUsername_WhenNotInEmployeeList()
    {
        await using var ctx = CreateContext();
        ctx.JSInterop.Setup<string?>("localStorage.getItem", _ => true).SetResult("unknown-user");

        var cut = ctx.Render<Login>();
        await cut.WaitForAssertionAsync(() =>
        {
            cut.Instance.LoginModelValue.Username.ShouldBe(string.Empty);
            cut.Find($"[data-testid='{nameof(Login.Elements.RememberMySelection)}']")
                .HasAttribute("checked")
                .ShouldBeFalse();
        });
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        var store = new RememberSelectionUserSessionStore();
        var authProvider = new CustomAuthenticationStateProvider(store);
        ctx.Services.AddSingleton(authProvider);
        ctx.Services.AddSingleton<IHostEnvironment>(new RememberSelectionHostEnvironment("Testing"));
        return ctx;
    }

    private sealed class RememberSelectionUserSessionStore : IUserSessionStore
    {
        private string? _username;

        public Task<string?> GetAsync() => Task.FromResult(_username);

        public Task SetAsync(string username)
        {
            _username = username;
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _username = null;
            return Task.CompletedTask;
        }
    }

    private sealed class RememberSelectionHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
