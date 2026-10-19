using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class LoginRememberSelectionTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task Should_PreselectUsername_AfterSignInWithRememberChecked()
    {
        await Page.GotoAsync("/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId(nameof(Login.Elements.RememberMySelection)).CheckAsync();
        await Click(nameof(Login.Elements.LovejoyShortcut));
        await Page.WaitForURLAsync(url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var logout = Page.GetByText("Sign Out")
            .Or(Page.GetByText("Logout"))
            .Or(Page.GetByText("Log Out"))
            .Or(Page.GetByText("Log out"))
            .Or(Page.GetByText("Sign out"));
        await logout.First.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GotoAsync("/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var userSelect = Page.GetByTestId(nameof(Login.Elements.User));
        await Expect(userSelect).ToHaveValueAsync("tlovejoy");
        await Expect(Page.GetByTestId(nameof(Login.Elements.RememberMySelection))).ToBeCheckedAsync();
    }
}
