using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class NavMenuTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task Counter_NavLink_ShouldNotBeVisible()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The Counter nav link used data-testid="Counter"; verify it is absent from the DOM.
        var counterLink = Page.GetByTestId("Counter");
        await Expect(counterLink).Not.ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task FetchData_NavLink_ShouldBeVisible()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var weatherLink = Page.GetByTestId(nameof(NavMenu.Elements.Weather));
        await Expect(weatherLink).ToBeVisibleAsync();
        await Expect(weatherLink).ToHaveAttributeAsync("href", "fetchdata");
    }

    // Opening the page needs no chat client, unlike the chat tests in AiAgentPageTests, which are
    // Explicit: this is the test that opens the AI Agent screen in every run.
    [Test, Retry(2)]
    [Category("Smoke")]
    public async Task AiAgent_NavLink_ShouldOpenAiAgentPage()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.AiAgent));
        await Page.WaitForURLAsync("**/ai-agent");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId(nameof(ApplicationChat.Elements.ChatInput))).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(ApplicationChat.Elements.SendButton))).ToBeVisibleAsync();
    }
}
