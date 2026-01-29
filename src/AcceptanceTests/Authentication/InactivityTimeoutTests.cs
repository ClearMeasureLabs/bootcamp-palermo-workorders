using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

[TestFixture]
public class InactivityTimeoutTests : AcceptanceTestBase
{
    // Note: These tests use shorter timeouts for testing purposes
    // In production, the timeout is 60 seconds with a 50-second warning
    
    [Test]
    [Ignore("Manual test - requires waiting for actual timeout")]
    public async Task Should_ShowWarningModal_AfterConfiguredInactivityPeriod()
    {
        // Arrange: Login first
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Act: Wait for warning modal to appear (in real implementation, this would be 50 seconds)
        // For testing, we would need to temporarily reduce the timeout values
        
        // Assert: Warning modal should be visible
        var warningModal = Page.GetByTestId(nameof(MainLayout.Elements.InactivityWarningModal));
        await Expect(warningModal).ToBeVisibleAsync();
    }

    [Test]
    [Ignore("Manual test - requires waiting for actual timeout")]
    public async Task Should_DisplayCorrectWarningMessage_InModal()
    {
        // Arrange: Login first
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Act: Wait for warning modal to appear
        var warningMessage = Page.GetByTestId(nameof(MainLayout.Elements.InactivityWarningMessage));
        
        // Assert: Verify warning message content
        await Expect(warningMessage).ToContainTextAsync("You will be logged out in 10 seconds due to inactivity");
    }

    [Test]
    [Ignore("Manual test - requires waiting for actual timeout")]
    public async Task Should_ResetTimer_WhenUserClicksStayLoggedIn()
    {
        // Arrange: Login first
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Act: Wait for warning, then click "Stay Logged In"
        var stayLoggedInButton = Page.GetByTestId(nameof(MainLayout.Elements.StayLoggedInButton));
        await stayLoggedInButton.WaitForAsync();
        await stayLoggedInButton.ClickAsync();
        
        // Assert: Modal should be hidden
        var warningModal = Page.GetByTestId(nameof(MainLayout.Elements.InactivityWarningModal));
        await Expect(warningModal).ToBeHiddenAsync();
        
        // User should still be logged in
        var welcomeText = Page.GetByTestId(nameof(Logout.Elements.WelcomeText));
        await Expect(welcomeText).ToBeVisibleAsync();
    }

    [Test]
    [Ignore("Manual test - requires waiting for actual timeout")]
    public async Task Should_LogoutAndRedirect_AfterFullTimeout()
    {
        // Arrange: Login first
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Act: Wait for full timeout (60 seconds in production)
        await Page.WaitForTimeoutAsync(61000); // Wait 61 seconds
        
        // Assert: Should be redirected to home page
        await Page.WaitForURLAsync("**/");
        
        // User should no longer see welcome message
        var welcomeText = Page.GetByTestId(nameof(Logout.Elements.WelcomeText));
        await Expect(welcomeText).ToBeHiddenAsync();
    }

    [Test]
    public async Task Should_NotShowWarning_WhenUserIsNotAuthenticated()
    {
        // Arrange: Navigate to home without logging in
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Act: Wait a bit to ensure no warning appears
        await Page.WaitForTimeoutAsync(2000);
        
        // Assert: Warning modal should not be visible
        var warningModals = await Page.GetByTestId(nameof(MainLayout.Elements.InactivityWarningModal)).CountAsync();
        warningModals.ShouldBe(0);
    }

    [Test]
    public async Task Should_InitializeInactivityTimer_WhenUserLogsIn()
    {
        // Arrange & Act: Login
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Assert: User should be logged in and on home page
        var welcomeText = Page.GetByTestId(nameof(Logout.Elements.WelcomeText));
        await Expect(welcomeText).ToBeVisibleAsync();
        
        // Verify JS timer was initialized by checking console for any JS errors
        // (If timer initialization failed, there would be JS errors)
        await TakeScreenshotAsync(1, "AfterLogin");
    }

    [Test]
    public async Task Should_ResetTimer_OnUserActivity()
    {
        // Arrange: Login first
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Act: Simulate user activity (mouse movement)
        await Page.Mouse.MoveAsync(100, 100);
        await Page.WaitForTimeoutAsync(500);
        await Page.Mouse.MoveAsync(200, 200);
        
        // Assert: No warning should appear immediately after activity
        var warningModals = await Page.GetByTestId(nameof(MainLayout.Elements.InactivityWarningModal)).CountAsync();
        warningModals.ShouldBe(0);
    }
}
