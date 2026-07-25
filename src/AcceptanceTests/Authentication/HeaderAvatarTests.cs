using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

[TestFixture]
public class HeaderAvatarTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldDisplayAvatarWithInitialsAfterLogin()
    {
        CurrentUser = CreateNamedEmployee("jdoe", "Jane", "Doe");
        await LoginAsCurrentUser();

        var avatar = Page.GetByTestId(nameof(UserAvatar.Elements.UserAvatar));
        await Expect(avatar).ToBeVisibleAsync();
        await Expect(avatar).ToHaveTextAsync("JD");
        await Expect(avatar).ToHaveAttributeAsync("aria-label", "Signed in as Jane Doe");
    }

    [Test, Retry(2)]
    public async Task ShouldRemoveAvatarAfterLogout()
    {
        CurrentUser = CreateNamedEmployee("jdoe", "Jane", "Doe");
        await LoginAsCurrentUser();

        var avatar = Page.GetByTestId(nameof(UserAvatar.Elements.UserAvatar));
        await Expect(avatar).ToBeVisibleAsync();

        await Click(nameof(Logout.Elements.LogoutLink));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(avatar).ToHaveCountAsync(0);
    }

    [Test, Retry(2)]
    public async Task ShouldMaintainAvatarLegibilityOnNarrowViewport()
    {
        await Page.SetViewportSizeAsync(375, 667);
        CurrentUser = CreateNamedEmployee("jdoe", "Jane", "Doe");
        await LoginAsCurrentUser();

        var avatar = Page.GetByTestId(nameof(UserAvatar.Elements.UserAvatar));
        await Expect(avatar).ToBeVisibleAsync();
        await Expect(avatar).ToHaveTextAsync("JD");

        var box = await avatar.BoundingBoxAsync();
        box.ShouldNotBeNull();
        box!.Width.ShouldBeGreaterThan(30);
        box.Height.ShouldBeGreaterThan(30);
    }

    [Test, Retry(2)]
    public async Task ShouldShowCorrectInitialsForSingleNameUser()
    {
        CurrentUser = CreateNamedEmployee("homer", "Homer", "");
        await LoginAsCurrentUser();

        var avatar = Page.GetByTestId(nameof(UserAvatar.Elements.UserAvatar));
        await Expect(avatar).ToHaveTextAsync("H");
    }

    [Test, Retry(2)]
    public async Task ShouldShowUsernameInitialsFallback()
    {
        CurrentUser = CreateNamedEmployee("hsimpson", "", "");
        await LoginAsCurrentUser();

        var avatar = Page.GetByTestId(nameof(UserAvatar.Elements.UserAvatar));
        await Expect(avatar).ToHaveTextAsync("HS");
    }

    private static Employee CreateNamedEmployee(string userName, string firstName, string lastName)
    {
        using var context = TestHost.NewDbContext();
        var employee = new Employee(userName, firstName, lastName, $"{userName}@test.com");
        employee.AddRole(new Role("admin", true, true));
        context.Add(employee);
        context.SaveChanges();
        return employee;
    }
}
