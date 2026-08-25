using System.Globalization;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Login = ClearMeasure.Bootcamp.UI.Shared.Pages.Login;
using NavMenu = ClearMeasure.Bootcamp.UI.Shared.NavMenu;

namespace ClearMeasure.Bootcamp.AcceptanceTests.AIAgents;

/// <summary>
/// Required LLM acceptance: Lovejoy schedules ten Saturday mows for Willie via AI Agent.
/// Skips when no chat client is configured.
/// </summary>
[TestFixture]
public class SaturdayMowSchedulingAgentTests : AcceptanceTestBase
{
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
                ["runId"] = "pre-fix"
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
    // #endregion

    [SetUp]
    public async Task EnsureLlmAvailable()
    {
        await SkipIfNoChatClient();
    }

    [Test]
    [Retry(2)]
    public async Task ShouldCreateTenSaturdayMowsForWillieViaAiAgent()
    {
        // #region agent log
        var apiStatuses = new List<int>();
        Page.Response += (_, response) =>
        {
            if (response.Url.Contains("blazor-wasm-single-api", StringComparison.OrdinalIgnoreCase)
                || response.Url.Contains("/api/", StringComparison.OrdinalIgnoreCase))
            {
                apiStatuses.Add(response.Status);
            }
        };
        AgentLog("E", "SaturdayMowSchedulingAgentTests.cs:entry", "Test entry", new
        {
            baseUrl = ServerFixture.ApplicationBaseUrl,
            startLocal = ServerFixture.StartLocalServer
        });
        // #endregion

        await Page.GotoAsync("/login");
        var shortcut = Page.GetByTestId(nameof(Login.Elements.LovejoyShortcut));
        await shortcut.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000
        });
        // #region agent log
        var beforeClick = await CaptureLoginDiagAsync("before-lovejoy-click");
        AgentLog("A", "SaturdayMowSchedulingAgentTests.cs:beforeClick", "Login diag before Lovejoy click", beforeClick);
        AgentLog("B", "SaturdayMowSchedulingAgentTests.cs:beforeClickApi", "API statuses before click", new
        {
            statuses = apiStatuses.ToArray(),
            count429 = apiStatuses.Count(s => s == 429)
        });
        var shortcutVisible = await shortcut.IsVisibleAsync();
        // #endregion
        await Click(nameof(Login.Elements.LovejoyShortcut));
        // #region agent log
        AgentLog("C", "SaturdayMowSchedulingAgentTests.cs:afterClickHelper", "Click helper returned", new
        {
            shortcutVisibleBeforeClick = shortcutVisible,
            url = Page.Url
        });
        // #endregion
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        // #region agent log
        // Brief settle so Blazor can re-render LoginDiag / NavigateTo after onclick.
        await Page.WaitForTimeoutAsync(500);
        var afterClick = await CaptureLoginDiagAsync("after-lovejoy-click");
        AgentLog("A", "SaturdayMowSchedulingAgentTests.cs:afterClick", "Login diag after Lovejoy click", afterClick);
        AgentLog("B", "SaturdayMowSchedulingAgentTests.cs:afterClickApi", "API statuses after click", new
        {
            statuses = apiStatuses.ToArray(),
            count429 = apiStatuses.Count(s => s == 429)
        });
        AgentLog("D", "SaturdayMowSchedulingAgentTests.cs:afterClickAuth", "Auth/welcome presence", new
        {
            url = Page.Url,
            welcomeCount = afterClick.GetValueOrDefault("welcomeCount"),
            loginLinkCount = afterClick.GetValueOrDefault("loginLinkCount"),
            authOutcome = afterClick.GetValueOrDefault("authOutcome"),
            error = afterClick.GetValueOrDefault("error")
        });
        // #endregion
        await Expect(Page.GetByTestId(nameof(Logout.Elements.WelcomeText)))
            .ToHaveTextAsync("Welcome tlovejoy!");

        await Click(nameof(NavMenu.Elements.AiAgent));
        await Page.WaitForURLAsync("**/ai-agent");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        const string prompt =
            "Create 10 work orders for Groundskeeper Willie MacDougal to mow the grass, " +
            "one per week for the next 10 Saturdays, each due that Saturday (the day before Sunday service). " +
            "In your reply list every work order number and due date.";

        await Input(nameof(ApplicationChat.Elements.ChatInput), prompt);
        await Click(nameof(ApplicationChat.Elements.SendButton));

        var expectedDates = WorkOrderToolsComingSaturdays();
        var firstDateText = expectedDates[0].ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        // Wait for the assistant bubble to contain tool-backed content (not merely to exist).
        // AiMessage1 can appear before the full reply is meaningful if the model echoes early.
        var aiMessage = Page.GetByTestId(nameof(ApplicationChat.Elements.AiMessage) + "1");
        await Expect(aiMessage).ToContainTextAsync(
            firstDateText,
            new LocatorAssertionsToContainTextOptions { Timeout = 180_000 });
        await Expect(Page.GetByTestId(nameof(ApplicationChat.Elements.LoadingIndicator)))
            .ToHaveCountAsync(0);

        var chatText = await Page.GetByTestId(nameof(ApplicationChat.Elements.ChatHistory)).InnerTextAsync();
        chatText.ShouldNotBeNullOrEmpty();
        var aiText = await aiMessage.InnerTextAsync();
        aiText.ShouldNotBeNullOrEmpty();

        foreach (var date in expectedDates)
        {
            var iso = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            aiText.ShouldContain(iso);
            chatText.ShouldContain(iso);
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var all = await bus.Send(new WorkOrderSpecificationQuery());
        var created = all
            .Where(wo => wo.Creator?.UserName == "tlovejoy"
                         && wo.Assignee?.UserName == "gwillie"
                         && wo.DueDate != null
                         && expectedDates.Contains(wo.DueDate.Value))
            .OrderBy(wo => wo.DueDate)
            .ToList();

        created.Count.ShouldBeGreaterThanOrEqualTo(10,
            $"Expected at least 10 Saturday mows for gwillie. Chat: {chatText}");

        var matchingSet = created
            .GroupBy(wo => wo.DueDate)
            .Select(g => g.First())
            .OrderBy(wo => wo.DueDate)
            .Take(10)
            .ToList();
        matchingSet.Count.ShouldBe(10);
        matchingSet.Select(wo => wo.DueDate!.Value).ShouldBe(expectedDates);

        foreach (var wo in matchingSet)
        {
            chatText.ShouldContain(wo.Number!);
        }

        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search**");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        // Full Page.GotoAsync drops in-memory Blazor auth. Filter within the SPA instead.
        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        await Expect(assigneeSelect).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        await assigneeSelect.SelectOptionAsync("gwillie");
        await Page.Locator($"#{WorkOrderSearch.Elements.SearchButton}").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        foreach (var wo in matchingSet)
        {
            var cell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.DueDateCell) + wo.Number);
            await Expect(cell).ToBeAttachedAsync(new LocatorAssertionsToBeAttachedOptions { Timeout = 30_000 });
            await Expect(cell).ToContainTextAsync(
                wo.DueDate!.Value.ToString("MMM d, yyyy", CultureInfo.InvariantCulture));
        }
    }

    private static IReadOnlyList<DateOnly> WorkOrderToolsComingSaturdays()
    {
        var first = ChurchTimeZone.ComingSaturday(TimeProvider.System);
        return Enumerable.Range(0, 10).Select(i => first.AddDays(7 * i)).ToList();
    }
}
