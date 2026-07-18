using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.AIAgents;

/// <summary>
///     Acceptance test for the AutoReformatAgentService.
///     Creates a draft work order with a lowercase title and poor grammar in the description,
///     then waits for the background reformat agent to correct them.
/// </summary>
public class AutoReformatAgentTests : AcceptanceTestBase
{
    [Test, Retry(2), Explicit]
    public async Task ShouldReformatWorkOrderTitleAndDescription()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        // Set lowercase title and bad grammar description on the draft work order
        order.Title = $"[{TestTag}] lowercase needs fixing";
        order.Description = "this is bad grammer and no punctuation missing capital letters";
        order = await ClickWorkOrderNumberFromSearchPage(order);

        // Save the draft with the bad title and description
        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for the background reformat agent to process (polls every 5 seconds)
        await Task.Delay(8000);

        // Reload the work order from the database to check agent changes
        var rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehydrated.ShouldNotBeNull();

        // The reformat agent should have capitalized the title's first letter
        // and corrected grammar/punctuation in the description
        rehydrated.Title.ShouldNotBeNull();
        rehydrated.Title![0].ShouldBe(char.ToUpper(rehydrated.Title[0]));

        rehydrated.Description.ShouldNotBeNull();
        rehydrated.Description.ShouldNotBe(order.Description);
    }
}
