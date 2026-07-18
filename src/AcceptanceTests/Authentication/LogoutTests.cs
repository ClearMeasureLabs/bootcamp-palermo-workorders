using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

public class LogoutTests : AcceptanceTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await LoginAsCurrentUser();
    }

    [Test, Retry(2)]
    public async Task ShouldLogout()
    {
        var newLink = Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder));
        await newLink.WaitForAsync();
        await Expect(newLink).ToBeVisibleAsync();

        await Click(nameof(Logout.Elements.LogoutLink));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var loginLink = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(loginLink).ToBeVisibleAsync();
        
        var newWorkOrderLink = Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder));
        await Expect(newWorkOrderLink).ToBeHiddenAsync();
    }
}