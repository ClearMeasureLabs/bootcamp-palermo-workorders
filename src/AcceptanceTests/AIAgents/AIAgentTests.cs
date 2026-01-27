using ClearMeasure.Bootcamp.UI.Shared.Components;

namespace ClearMeasure.Bootcamp.AcceptanceTests.AIAgents;

[TestFixture]
public class AIAgentTests : AcceptanceTestBase
{
    [Test]
    public async Task AIAgentButtons_AreGreen()
    {
        // Login as current user
        await LoginAsCurrentUser();

        // Navigate to AI agents interface (via work order with chat)
        var workOrder = await CreateAndSaveNewWorkOrder();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1);

        // Verify the "Send" button in the chat interface is green
        var sendButton = Page.GetByTestId(nameof(WorkOrderChat.Elements.SendButton));
        
        if (await sendButton.CountAsync() > 0)
        {
            await Expect(sendButton).ToBeVisibleAsync();
            var backgroundColor = await sendButton.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundColor");
            backgroundColor.ShouldContain("34, 197, 94"); // RGB for green (#22c55e)
            await TakeScreenshotAsync(2);
        }
    }
}
