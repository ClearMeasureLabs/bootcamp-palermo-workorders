using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Client.Pages;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

[TestFixture]
public class ClientHealthCheckPageTests
{
    [Test]
    public async Task ClientHealthCheck_ShouldRenderStatus_WhenHealthServiceReturnsReport()
    {
        await using var ctx = CreateContextWithHealthChecks();

        var component = ctx.Render<ClientHealthCheck>();

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{ClientHealthCheck.Elements.Status}']").ShouldNotBeNull();
        });
        component.Markup.ShouldNotContain("Loading health report");
    }

    [Test]
    public async Task DetailedClientHealthCheck_ShouldRenderStatus_WhenHealthServiceReturnsReport()
    {
        await using var ctx = CreateContextWithHealthChecks();

        var component = ctx.Render<DetailedClientHealthCheck>();

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{DetailedClientHealthCheck.Elements.Status}']").ShouldNotBeNull();
        });
        component.Markup.ShouldNotContain("Loading detailed health report");
    }

    private static BunitContext CreateContextWithHealthChecks()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks();
        var provider = services.BuildServiceProvider();

        var ctx = new BunitContext();
        ctx.Services.AddSingleton(provider.GetRequiredService<HealthCheckService>());
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        return ctx;
    }
}
