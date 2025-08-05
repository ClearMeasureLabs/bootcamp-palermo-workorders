using ClearMeasure.Bootcamp.UI.Client.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class ClientHealthCheckTests : AcceptanceTestBase
{
    [Test]
    public async Task FirstStartShouldValidateClientHealthChecks()
    {
        await Page.GotoAsync("/_clienthealthcheck");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var statusSpan = Page.GetByTestId(nameof(ClientHealthCheck.Elements.Status));
        var innerTextAsync = await statusSpan.InnerTextAsync();
        innerTextAsync.ShouldBe(nameof(HealthStatus.Healthy));
    }

    [Test]
    public async Task Should_NavigateToHealthCheck_WhenGearIconClicked()
    {
        // Arrange
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        await Click(nameof(HealthCheckLink.Elements.HealthCheckLink));

        // Assert
        await Page.WaitForURLAsync("**/_clienthealthcheck");
        Page.Url.ShouldContain("/_clienthealthcheck");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var statusSpan = Page.GetByTestId(nameof(ClientHealthCheck.Elements.Status));
        var innerTextAsync = await statusSpan.InnerTextAsync();
        innerTextAsync.ShouldBe(nameof(HealthStatus.Healthy));
    }

    [Test]
    public async Task FirstStartShouldValidateServerHealthChecks()
    {
        await Page.GotoAsync("/_healthcheck");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        (await Page.ContentAsync()).ShouldContain("Healthy");
    }
}