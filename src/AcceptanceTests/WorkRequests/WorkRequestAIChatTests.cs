using ClearMeasure.Bootcamp.UI.Shared.Components;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkRequests;

public class WorkRequestAiChatTests : AcceptanceTestBase
{
    [SetUp]
    public async Task EnsureLlmAvailable()
    {
        await SkipIfNoChatClient();
    }

    [Test, Retry(2)]
    public async Task ShouldSendChatMessageAndReceiveResponse()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();
        order = await ClickWorkRequestNumberFromSearchPage(order);
        order = await AssignExistingWorkRequest(order, CurrentUser.UserName);
        order = await ClickWorkRequestNumberFromSearchPage(order);

        // Input prompt and send message
        const string prompt = "tell me about this work request";
        await Input(nameof(WorkRequestChat.Elements.ChatInput), prompt);
        await Click(nameof(WorkRequestChat.Elements.SendButton));

        // Wait for the AI response message to appear in the DOM
        var aiMessage = Page.GetByTestId(nameof(WorkRequestChat.Elements.AiMessage) + "1");
        await aiMessage.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });

        // Verify chat history is visible and contains messages
        var chatHistory = Page.GetByTestId(nameof(WorkRequestChat.Elements.ChatHistory));
        await Expect(chatHistory).ToBeVisibleAsync();

        // Verify chat history contains text content (messages were added)
        var chatHistoryText = await chatHistory.InnerTextAsync();
        chatHistoryText.ShouldNotBeNullOrEmpty();
        chatHistoryText.ShouldContain(prompt);
    }

    [Test, Retry(2)]
    public async Task ShouldRespondToChat()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();
        order = await ClickWorkRequestNumberFromSearchPage(order);
        order = await AssignExistingWorkRequest(order, CurrentUser.UserName);
        order = await ClickWorkRequestNumberFromSearchPage(order);

        // Input prompt and send message
        const string prompt = "what is the number of this work request?";
        await Input(nameof(WorkRequestChat.Elements.ChatInput), prompt);
        await Click(nameof(WorkRequestChat.Elements.SendButton));

        // Wait for the AI response message to appear in the DOM
        var aiMessage = Page.GetByTestId(nameof(WorkRequestChat.Elements.AiMessage) + "1");
        await aiMessage.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });

        // Verify chat history is visible and contains messages
        var chatHistory = Page.GetByTestId(nameof(WorkRequestChat.Elements.ChatHistory));
        await Expect(chatHistory).ToBeVisibleAsync();

        // User message is always in history; LLM may not echo the work request number verbatim.
        var chatHistoryText = await chatHistory.InnerTextAsync();
        chatHistoryText.ShouldNotBeNullOrEmpty();
        chatHistoryText.ShouldContain(prompt);

        var aiOnlyText = await aiMessage.InnerTextAsync();
        aiOnlyText.ShouldNotBeNullOrWhiteSpace();
    }

    [Test, Ignore("Not yet implemented")]
    public async Task ShouldListEmployees()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();
        order = await ClickWorkRequestNumberFromSearchPage(order);
        order = await AssignExistingWorkRequest(order, CurrentUser.UserName);
        order = await ClickWorkRequestNumberFromSearchPage(order);

        // Input prompt and send message
        const string prompt = "list employees";
        await Input(nameof(WorkRequestChat.Elements.ChatInput), prompt);
        await Click(nameof(WorkRequestChat.Elements.SendButton));

        // Wait for the AI response message to appear in the DOM
        var aiMessage = Page.GetByTestId(nameof(WorkRequestChat.Elements.AiMessage) + "1");
        await aiMessage.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });

        // Verify chat history is visible and contains messages
        var chatHistory = Page.GetByTestId(nameof(WorkRequestChat.Elements.ChatHistory));
        await Expect(chatHistory).ToBeVisibleAsync();

        // Verify chat history contains text content (messages were added)
        var chatHistoryText = await chatHistory.InnerTextAsync();
        chatHistoryText.ShouldNotBeNullOrEmpty();
        chatHistoryText.ShouldContain("Simpson");
    }
}