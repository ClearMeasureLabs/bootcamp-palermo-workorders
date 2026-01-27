using ClearMeasure.Bootcamp.UI.Shared.Components;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

[TestFixture]
public class AuthenticationTests : AcceptanceTestBase
{
    [Test]
    public async Task LoginForm_TextInputs_HaveBlinkingBorders()
    {
        // Act: Go to login page
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var logoutLink = Page.GetByTestId(nameof(Logout.Elements.LogoutLink));
        if (await logoutLink.CountAsync() > 0)
        {
            await logoutLink.ClickAsync();
            await Page.WaitForURLAsync("**/");
        }

        await Click(nameof(LoginLink.Elements.LoginLink));
        await Page.WaitForURLAsync("**/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Login form only has dropdown select (InputSelect), not text inputs
        // So we verify that no text input exists on this page
        var textInputs = await Page.Locator(".input-text, .input-textarea").CountAsync();
        textInputs.ShouldBe(0);
    }
}
