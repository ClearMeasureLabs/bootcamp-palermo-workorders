using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class ProfileTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task Should_NavigateToProfile_ViaNavMenu_WhenAuthenticated()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Profile));
        await Page.WaitForURLAsync("**/profile");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId(nameof(Profile.Elements.FullName))).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task Should_NavigateToProfile_ViaHeaderUsername_WhenAuthenticated()
    {
        await LoginAsCurrentUser();
        await Click(nameof(Logout.Elements.ProfileLink));
        await Page.WaitForURLAsync("**/profile");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId(nameof(Profile.Elements.Username))).ToContainTextAsync(CurrentUser.UserName);
    }

    [Test, Retry(2)]
    public async Task Should_DisplayIdentityFields_OnProfilePage()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Profile));
        await Page.WaitForURLAsync("**/profile");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId(nameof(Profile.Elements.FullName)))
            .ToContainTextAsync(CurrentUser.GetFullName());
        await Expect(Page.GetByTestId(nameof(Profile.Elements.Username)))
            .ToContainTextAsync(CurrentUser.UserName);
        await Expect(Page.GetByTestId(nameof(Profile.Elements.Email)))
            .ToContainTextAsync(CurrentUser.EmailAddress);
    }

    [Test, Retry(2)]
    public async Task Should_DisplayFormattedLastLogin_AfterRelog()
    {
        var previousLogin = DateTimeOffset.UtcNow.AddHours(-3);
        using (var context = TestHost.NewDbContext())
        {
            var employee = context.Set<Employee>().Single(e => e.UserName == CurrentUser.UserName);
            employee.LastLoginUtc = previousLogin;
            context.SaveChanges();
        }

        await LoginAsCurrentUser();

        await Page.GotoAsync("/login");
        await Page.WaitForURLAsync("**/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Select(nameof(Login.Elements.User), CurrentUser.UserName);
        await Click(nameof(Login.Elements.LoginButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(NavMenu.Elements.Profile));
        await Page.WaitForURLAsync("**/profile");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var lastLogin = Page.GetByTestId(nameof(Profile.Elements.LastLogin));
        await Expect(lastLogin).ToBeVisibleAsync();
        await Expect(lastLogin).Not.ToContainTextAsync("First login");
        await Expect(lastLogin).ToContainTextAsync("2026");
    }
}
