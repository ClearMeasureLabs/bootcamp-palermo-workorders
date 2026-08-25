using System.Globalization;
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
    [SetUp]
    public async Task EnsureLlmAvailable()
    {
        await SkipIfNoChatClient();
    }

    [Test]
    [Retry(2)]
    public async Task ShouldCreateTenSaturdayMowsForWillieViaAiAgent()
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
        await Expect(Page.GetByTestId(nameof(Logout.Elements.WelcomeText)))
            .ToHaveTextAsync("Welcome tlovejoy!");

        await Click(nameof(NavMenu.Elements.AiAgent));
        await Page.WaitForURLAsync("**/ai-agent");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        const string prompt =
            "Create 10 work orders for Groundskeeper Willie MacDougal (username gwillie) to mow the grass, " +
            "one per week for the next 10 Saturdays, each due that Saturday (the day before Sunday service). " +
            "Use create-dated-work-orders once with creatorUsername='tlovejoy', assigneeUsername='gwillie', " +
            "title='Mow the grass', description='Weekly Saturday lawn mowing', saturdayCount=10. " +
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
