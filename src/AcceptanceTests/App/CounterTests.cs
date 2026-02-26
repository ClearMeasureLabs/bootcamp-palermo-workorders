using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class CounterTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task Should_DisplayCounterPage_WhenNavigatingFromNav()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Counter));
        await Page.WaitForURLAsync("**/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var valueLocator = Page.GetByTestId(nameof(Counter.Elements.CounterValue));
        await Expect(valueLocator).ToBeVisibleAsync();
        await Expect(valueLocator).ToHaveTextAsync("0");
    }

    [Test, Retry(2)]
    public async Task Should_ShowInitialCountZero_WhenOpeningCounterPage()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var valueLocator = Page.GetByTestId(nameof(Counter.Elements.CounterValue));
        await Expect(valueLocator).ToBeVisibleAsync();
        await Expect(valueLocator).ToHaveTextAsync("0");
    }

    [Test, Retry(2)]
    public async Task Should_IncrementCount_WhenClickingAddActivity()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(Counter.Elements.IncrementButton));
        await Expect(Page.GetByTestId(nameof(Counter.Elements.CounterValue))).ToHaveTextAsync("1");

        await Click(nameof(Counter.Elements.IncrementButton));
        await Expect(Page.GetByTestId(nameof(Counter.Elements.CounterValue))).ToHaveTextAsync("2");
    }

    [Test, Retry(2)]
    public async Task Should_ResetCountToZero_WhenClickingResetCounter()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(Counter.Elements.IncrementButton));
        await Click(nameof(Counter.Elements.IncrementButton));
        await Expect(Page.GetByTestId(nameof(Counter.Elements.CounterValue))).ToHaveTextAsync("2");

        await Click(nameof(Counter.Elements.ResetButton));
        await Expect(Page.GetByTestId(nameof(Counter.Elements.CounterValue))).ToHaveTextAsync("0");
    }

    [Test, Retry(2)]
    public async Task Should_DisplayPageTitle_WhenOnCounterPage()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex("Counter.*Church Activity Tracker"));
    }
}
