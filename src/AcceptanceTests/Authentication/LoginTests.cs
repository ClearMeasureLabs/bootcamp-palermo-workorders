using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

[TestFixture]
public class LoginTests : AcceptanceTestBase
{
    /// <summary>
    /// The employee &lt;option&gt; elements are rendered only after the Blazor WASM app
    /// boots interactively and the employee list loads. WaitForLoadStateAsync(NetworkIdle)
    /// does not guarantee that render has happened, so wait for the option itself.
    /// Options live inside a closed &lt;select&gt; and therefore have no bounding box —
    /// they are never "visible" to Playwright, so wait for Attached, not Visible.
    /// </summary>
    private static Task WaitForEmployeeOptionsRenderedAsync(ILocator employeeOption) =>
        employeeOption.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = 90_000
        });

    // #region agent log
    private static void AgentLog(string hypothesisId, string location, string message, object data)
    {
        try
        {
            var payload = new Dictionary<string, object?>
            {
                ["hypothesisId"] = hypothesisId,
                ["location"] = location,
                ["message"] = message,
                ["data"] = data,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["runId"] = "deterministic-repro-9086"
            };
            File.AppendAllText(
                "/opt/cursor/logs/debug.log",
                JsonSerializer.Serialize(payload) + "\n");
        }
        catch
        {
            // Diagnostic only — never fail the test on log I/O.
        }
    }

    private async Task<Dictionary<string, string?>> CaptureLoginDiagAsync(string phase)
    {
        var diag = Page.GetByTestId(nameof(Login.Elements.LoginDiag));
        var diagCount = await diag.CountAsync();
        string? alertText = null;
        var alert = Page.Locator(".alert-danger");
        if (await alert.CountAsync() > 0)
        {
            alertText = await alert.First.InnerTextAsync();
        }

        var attrs = new Dictionary<string, string?>
        {
            ["phase"] = phase,
            ["url"] = Page.Url,
            ["diagCount"] = diagCount.ToString(),
            ["welcomeCount"] = (await Page.GetByTestId(nameof(Logout.Elements.WelcomeText)).CountAsync()).ToString(),
            ["loginLinkCount"] = (await Page.GetByTestId(nameof(LoginLink.Elements.LoginLink)).CountAsync()).ToString(),
            ["alertText"] = alertText,
            ["employeeOptionCount"] = (await Page.GetByTestId(nameof(Login.Elements.User))
                .Locator("option[value]:not([value=''])").CountAsync()).ToString()
        };
        if (diagCount > 0)
        {
            attrs["employeeCount"] = await diag.GetAttributeAsync("data-employee-count");
            attrs["hasTlovejoy"] = await diag.GetAttributeAsync("data-has-tlovejoy");
            attrs["loadCompleted"] = await diag.GetAttributeAsync("data-load-completed");
            attrs["error"] = await diag.GetAttributeAsync("data-error");
            attrs["authOutcome"] = await diag.GetAttributeAsync("data-auth-outcome");
            attrs["username"] = await diag.GetAttributeAsync("data-username");
        }

        return attrs;
    }

    /// <summary>
    /// Diagnostic-only (#9086): delay EmployeeGetAllQuery so Lovejoy shortcut can be clicked
    /// while <c>Employees</c> is still empty — reproduces WelcomeText missing / stay on /login.
    /// </summary>
    [Test]
    [Explicit("Diagnostic reproduction for #9086 — temporary; not a permanent gate")]
    [Category("Diagnostic9086")]
    public async Task Should_FailWelcomeText_WhenLovejoyClickedBeforeEmployeesLoaded()
    {
        const int employeeQueryDelayMs = 15_000;
        var delayedEmployeeQueries = 0;

        await Page.RouteAsync("**/*blazor-wasm-single-api*", async route =>
        {
            if (!string.Equals(route.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                await route.ContinueAsync();
                return;
            }

            var postData = route.Request.PostData ?? string.Empty;
            if (postData.Contains("EmployeeGetAllQuery", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref delayedEmployeeQueries);
                await Task.Delay(employeeQueryDelayMs);
            }

            await route.ContinueAsync();
        });

        AgentLog("A", "LoginTests.cs:reproEntry", "Deterministic Lovejoy-before-employees repro start", new
        {
            employeeQueryDelayMs,
            baseUrl = ServerFixture.ApplicationBaseUrl,
            startLocal = ServerFixture.StartLocalServer
        });

        await Page.GotoAsync("/login");
        var shortcut = Page.GetByTestId(nameof(Login.Elements.LovejoyShortcut));
        await shortcut.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000
        });

        var beforeClick = await CaptureLoginDiagAsync("before-lovejoy-click");
        AgentLog("A", "LoginTests.cs:beforeClick", "Login diag before Lovejoy click (employees should still be empty)", beforeClick);

        await Click(nameof(Login.Elements.LovejoyShortcut));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500);

        var afterClick = await CaptureLoginDiagAsync("after-lovejoy-click");
        AgentLog("A", "LoginTests.cs:afterClick", "Login diag after Lovejoy click (expect reject / still on login)", afterClick);
        AgentLog("A", "LoginTests.cs:reproSummary", "Delay and auth summary", new
        {
            delayedEmployeeQueries,
            url = afterClick.GetValueOrDefault("url"),
            employeeCount = afterClick.GetValueOrDefault("employeeCount"),
            loadCompleted = afterClick.GetValueOrDefault("loadCompleted"),
            hasTlovejoy = afterClick.GetValueOrDefault("hasTlovejoy"),
            authOutcome = afterClick.GetValueOrDefault("authOutcome"),
            error = afterClick.GetValueOrDefault("error"),
            alertText = afterClick.GetValueOrDefault("alertText"),
            welcomeCount = afterClick.GetValueOrDefault("welcomeCount")
        });

        // Same assertion as CI failure / SaturdayMow line 38 — expected to fail under the delay.
        await Expect(Page.GetByTestId(nameof(Logout.Elements.WelcomeText)))
            .ToHaveTextAsync(
                "Welcome tlovejoy!",
                new LocatorAssertionsToHaveTextOptions { Timeout = 5_000 });
    }
    // #endregion

    [Test, Retry(2)]
    public void VerifySetup()
    {
        var homer = TestHost.NewDbContext().Set<Employee>().Single(employee =>
            employee.UserName == "hsimpson");

        homer.ShouldNotBeNull();
    }

    [Test, Retry(2)]
    public async Task Should_DisplayUppercaseNames_InLoginDropdown()
    {
        await Page.GotoAsync("/login");

        var userSelect = Page.GetByTestId(nameof(Login.Elements.User));
        var homerOption = userSelect.Locator("option[value='hsimpson']");
        await WaitForEmployeeOptionsRenderedAsync(homerOption);

        var placeholderOption = userSelect.Locator("option[value='']");
        await Expect(placeholderOption).ToHaveTextAsync("-- Select a parishioner or staff member --");
        await Expect(homerOption).ToHaveTextAsync("HOMER SIMPSON");
    }

    [Test, Retry(2)]
    public async Task Should_ExposeUserSelectIdMatchingLabelFor()
    {
        await Page.GotoAsync("/login");

        var userSelect = Page.GetByTestId(nameof(Login.Elements.User));
        var homerOption = userSelect.Locator("option[value='hsimpson']");
        await WaitForEmployeeOptionsRenderedAsync(homerOption);

        await Expect(userSelect).ToHaveAttributeAsync("id", nameof(Login.Elements.User));
        await Expect(Page.Locator($"label[for='{Login.Elements.User}']")).ToBeAttachedAsync();
    }

    [Test, Retry(2)]
    public async Task Should_LoginSuccessfully_UsingUsernameValue_NotDisplayLabel()
    {
        await Page.GotoAsync("/login");

        var userSelect = Page.GetByTestId(nameof(Login.Elements.User));
        var homerOption = userSelect.Locator("option[value='hsimpson']");
        await WaitForEmployeeOptionsRenderedAsync(homerOption);
        await Expect(homerOption).ToHaveTextAsync("HOMER SIMPSON");

        await Select(nameof(Login.Elements.User), "hsimpson");
        await Click(nameof(Login.Elements.LoginButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var welcomeTextLocator = Page.GetByTestId(nameof(Logout.Elements.WelcomeText));
        await Expect(welcomeTextLocator).ToHaveTextAsync("Welcome hsimpson!");
    }

    [Test, Retry(2)]
    public async Task LoginWithUsernameOnlyForwardsToHomePage()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1);

        var logoutLink = Page.GetByTestId(nameof(Logout.Elements.LogoutLink));
        if (await logoutLink.CountAsync() > 0)
        {
            await logoutLink.ClickAsync();
            await Page.WaitForURLAsync("**/");
        }

        await Click(nameof(LoginLink.Elements.LoginLink));
        await Page.WaitForURLAsync("**/login");
        await TakeScreenshotAsync(2);

        await Select(nameof(Login.Elements.User), "hsimpson");
        await TakeScreenshotAsync(3);

        await Click(nameof(Login.Elements.LoginButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(4);

        var welcomeTextLocator = Page.GetByTestId(nameof(Logout.Elements.WelcomeText));
        await Expect(welcomeTextLocator).ToHaveTextAsync("Welcome hsimpson!");
    }

    [Test, Retry(2)]
    public async Task Should_ShowLovejoyShortcut_OnLogin_WithoutSelectingMember()
    {
        await Page.GotoAsync("/login");

        var shortcut = Page.GetByTestId(nameof(Login.Elements.LovejoyShortcut));
        await shortcut.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000
        });

        await Expect(shortcut).ToBeVisibleAsync();
        await Expect(shortcut).ToHaveTextAsync("Log in as Timothy Lovejoy");
    }

    [Test, Retry(2)]
    public async Task Should_LoginAsTlovejoy_WhenLovejoyShortcutClicked()
    {
        await Page.GotoAsync("/login");

        var shortcut = Page.GetByTestId(nameof(Login.Elements.LovejoyShortcut));
        await shortcut.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000
        });

        await Click(nameof(Login.Elements.LovejoyShortcut));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var welcomeTextLocator = Page.GetByTestId(nameof(Logout.Elements.WelcomeText));
        await Expect(welcomeTextLocator).ToHaveTextAsync("Welcome tlovejoy!");
    }
}
