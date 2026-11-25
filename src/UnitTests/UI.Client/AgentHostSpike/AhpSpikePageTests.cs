using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Client.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client.AgentHostSpike;

/// <summary>
/// bUnit tests for the AHP spike page: disconnected defaults, connection-failure logging,
/// and the periodic render loop.
/// </summary>
[TestFixture]
public class AhpSpikePageTests
{
    [Test]
    public async Task Should_RenderDisconnectedStateAndProbes_When_PageLoads()
    {
        await using var ctx = await CreateContextAsync();

        var component = ctx.Render<AhpSpike>();

        component.Find(TestId(AhpSpike.Elements.ConnectionStatus)).TextContent.ShouldBe("Disconnected");
        component.Find(TestId(AhpSpike.Elements.Probes)).TextContent.ShouldContain("OperatingSystem.IsBrowser()");
        component.Find(TestId(AhpSpike.Elements.Probes)).TextContent.ShouldContain("new Thread().Start()");
        component.Find(TestId(AhpSpike.Elements.StartButton)).HasAttribute("disabled").ShouldBeTrue();
        component.Find(TestId(AhpSpike.Elements.ReconnectButton)).HasAttribute("disabled").ShouldBeTrue();
    }

    [Test]
    public async Task Should_LogConnectionFailure_When_NoHostIsListening()
    {
        await using var ctx = await CreateContextAsync();

        var component = ctx.Render<AhpSpike>();
        await component.Find(TestId(AhpSpike.Elements.ConnectButton)).ClickAsync(new MouseEventArgs());
        await component.WaitForAssertionAsync(() =>
            component.Find(TestId(AhpSpike.Elements.Log)).TextContent.ShouldNotBeEmpty(), TimeSpan.FromSeconds(10));

        component.Find(TestId(AhpSpike.Elements.ConnectionStatus)).TextContent.ShouldBe("Disconnected");
    }

    [Test]
    public async Task Should_KeepRendering_When_RenderLoopTicks()
    {
        await using var ctx = await CreateContextAsync();

        var component = ctx.Render<AhpSpike>();
        await Task.Delay(250);

        component.Markup.ShouldContain("Spike S1");
    }

    private static string TestId(AhpSpike.Elements element) => $"[data-testid='{element}']";

    private static async Task<BunitContext> CreateContextAsync()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        var store = new StubUserSessionStore { Username = "alice" };
        var authProvider = new CustomAuthenticationStateProvider(store);
        await authProvider.Login("alice");
        ctx.Services.AddSingleton<AuthenticationStateProvider>(authProvider);
        return ctx;
    }
}
