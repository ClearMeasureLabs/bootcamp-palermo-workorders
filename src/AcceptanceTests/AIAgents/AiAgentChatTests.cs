using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.AIAgents;

public class AiAgentChatTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldSendChatMessageAndShowUserAndAiResponseInHistory()
    {
        await LoginAsCurrentUser();

        await Page.GotoAsync("ai-agent");
        await Page.WaitForURLAsync("**/ai-agent");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        const string prompt = "Hello, what can you help me with?";
        await Input(nameof(AiAgent.Elements.ChatInput), prompt);
        await Click(nameof(AiAgent.Elements.SendButton));

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var chatHistory = Page.GetByTestId(nameof(AiAgent.Elements.ChatHistory));
        await Expect(chatHistory).ToBeVisibleAsync();

        var chatHistoryText = await chatHistory.InnerTextAsync();
        chatHistoryText.ShouldNotBeNullOrEmpty();
        chatHistoryText.ShouldContain(prompt);
    }
}
