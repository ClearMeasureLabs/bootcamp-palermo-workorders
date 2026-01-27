using ClearMeasure.Bootcamp.UI.Shared.Components;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

[TestFixture]
public class LoginBlinkingBorderTests : AcceptanceTestBase
{
	[Test]
	public async Task ShouldShowBlinkingBorderOnLoginPageTextFields()
	{
		await Page.GotoAsync("/");
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await TakeScreenshotAsync(1, "HomePage");

		var logoutLink = Page.GetByTestId(nameof(Logout.Elements.LogoutLink));
		if (await logoutLink.CountAsync() > 0)
		{
			await logoutLink.ClickAsync();
			await Page.WaitForURLAsync("**/");
		}

		await Click(nameof(LoginLink.Elements.LoginLink));
		await Page.WaitForURLAsync("**/login");
		await TakeScreenshotAsync(2, "LoginPage");

		var textInputs = await Page.Locator("input[type='text'].form-control, textarea.form-control").AllAsync();

		foreach (var input in textInputs)
		{
			var animationName = await input.EvaluateAsync<string>("el => window.getComputedStyle(el).animationName");
			animationName.ShouldBe("blinkBorder");

			var animationDuration = await input.EvaluateAsync<string>("el => window.getComputedStyle(el).animationDuration");
			animationDuration.ShouldBe("2s");
		}

		await TakeScreenshotAsync(3, "LoginBlinkingBorderVerified");
	}
}
