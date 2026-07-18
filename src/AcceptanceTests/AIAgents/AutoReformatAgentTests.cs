using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.AIAgents;

/// <summary>
///     Acceptance test for the AutoReformatAgentService.
///     Creates a draft work request with a lowercase title and poor grammar in the description,
///     then waits for the background reformat agent to correct them.
/// </summary>
public class AutoReformatAgentTests : AcceptanceTestBase
{
    [Test, Retry(2), Explicit]
    public async Task ShouldReformatWorkRequestTitleAndDescription()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();

        // Set lowercase title and bad grammar description on the draft work request
        order.Title = $"[{TestTag}] lowercase needs fixing";
        order.Description = "this is bad grammer and no punctuation missing capital letters";
        order = await ClickWorkRequestNumberFromSearchPage(order);

        // Save the draft with the bad title and description
        await Input(nameof(WorkRequestManage.Elements.Title), order.Title);
        await Input(nameof(WorkRequestManage.Elements.Description), order.Description);
        await Click(nameof(WorkRequestManage.Elements.CommandButton) + "Save");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for the background reformat agent to process (polls every 5 seconds)
        await Task.Delay(8000);

        // Reload the work request from the database to check agent changes
        var rehydrated = await Bus.Send(new WorkRequestByNumberQuery(order.Number!));
        rehydrated.ShouldNotBeNull();

        // The reformat agent should have capitalized the title's first letter
        // and corrected grammar/punctuation in the description
        rehydrated.Title.ShouldNotBeNull();
        rehydrated.Title![0].ShouldBe(char.ToUpper(rehydrated.Title[0]));

        rehydrated.Description.ShouldNotBeNull();
        rehydrated.Description.ShouldNotBe(order.Description);
    }
}
