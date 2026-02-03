using ClearMeasure.Bootcamp.UI.Shared.Components;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

[TestFixture]
public class LoginButtonTests : AcceptanceTestBase
{
	[Test]
	public async Task LoginButton_WhenUserNotAuthenticated_BlinksRed()
	{
		// Navigate to application home page without authenticating
		await Page.GotoAsync("/");
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await TakeScreenshotAsync(1);

		// Locate the login button
		var loginButton = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
		await Expect(loginButton).ToBeVisibleAsync();

		// Verify button has blinking red visual effect (CSS animation class or style)
		var animationName = await loginButton.EvaluateAsync<string>("el => getComputedStyle(el).animationName");
		animationName.ShouldContain("blinkRed"); // The scoped CSS will have a pattern like "blinkRed-b-..."
	}

	[Test]
	public async Task LoginButton_WhenUserAuthenticated_DoesNotBlinkRed()
	{
		// Login as current user
		await LoginAsCurrentUser();
		await TakeScreenshotAsync(1);

		// Navigate to application home page
		await Page.GotoAsync("/");
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await TakeScreenshotAsync(2);

		// Locate the login button (or verify absence if button is replaced with user info)
		var loginButton = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
		var loginButtonCount = await loginButton.CountAsync();
		
		// Verify button does not exist when user is authenticated (it's replaced with logout button)
		loginButtonCount.ShouldBe(0);
	}

	[Test]
	public async Task LoginButton_WhenUserLogsOut_StartsBlinkingRed()
	{
		// Login as current user
		await LoginAsCurrentUser();
		await TakeScreenshotAsync(1);

		// Logout
		await Click(nameof(Logout.Elements.LogoutLink));
		await Page.WaitForURLAsync("**/");
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await TakeScreenshotAsync(2);

		// Locate the login button
		var loginButton = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
		await Expect(loginButton).ToBeVisibleAsync();

		// Verify button resumes blinking red effect
		var animationName = await loginButton.EvaluateAsync<string>("el => getComputedStyle(el).animationName");
		animationName.ShouldContain("blinkRed"); // The scoped CSS will have a pattern like "blinkRed-b-..."
	}
}
