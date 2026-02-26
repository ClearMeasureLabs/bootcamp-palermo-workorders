using ClearMeasure.Bootcamp.UI.Shared.Components;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderAiChatTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldSendChatMessageAndReceiveResponse()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        // Input prompt and send message
        const string prompt = "tell me about this work order";
        await Input(nameof(WorkOrderChat.Elements.ChatInput), prompt);
        await Click(nameof(WorkOrderChat.Elements.SendButton));

        // Wait for the AI response message to appear in the DOM
        var aiMessage = Page.GetByTestId(nameof(WorkOrderChat.Elements.AiMessage) + "1");
        await aiMessage.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });

        // Verify chat history is visible and contains messages
        var chatHistory = Page.GetByTestId(nameof(WorkOrderChat.Elements.ChatHistory));
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

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        // Input prompt and send message
        const string prompt = "what is the title of this work order?";
        await Input(nameof(WorkOrderChat.Elements.ChatInput), prompt);
        await Click(nameof(WorkOrderChat.Elements.SendButton));

        // Wait for the AI response message to appear in the DOM
        var aiMessage = Page.GetByTestId(nameof(WorkOrderChat.Elements.AiMessage) + "1");
        await aiMessage.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });

        // Verify chat history is visible and contains messages
        var chatHistory = Page.GetByTestId(nameof(WorkOrderChat.Elements.ChatHistory));
        await Expect(chatHistory).ToBeVisibleAsync();

        // Verify chat history contains text content (messages were added)
        var chatHistoryText = await chatHistory.InnerTextAsync();
        chatHistoryText.ShouldNotBeNullOrEmpty();
        chatHistoryText.ShouldContain(order.Title!);
    }

    [Test, Retry(2)]
    public async Task ShouldListEmployees()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        // Input prompt and send message
        const string prompt = "list employees";
        await Input(nameof(WorkOrderChat.Elements.ChatInput), prompt);
        await Click(nameof(WorkOrderChat.Elements.SendButton));

        // Wait for the AI response message to appear in the DOM
        var aiMessage = Page.GetByTestId(nameof(WorkOrderChat.Elements.AiMessage) + "1");
        await aiMessage.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });

        // Verify chat history is visible and contains messages
        var chatHistory = Page.GetByTestId(nameof(WorkOrderChat.Elements.ChatHistory));
        await Expect(chatHistory).ToBeVisibleAsync();

        // Verify chat history contains text content (messages were added)
        var chatHistoryText = await chatHistory.InnerTextAsync();
        chatHistoryText.ShouldNotBeNullOrEmpty();
        chatHistoryText.ShouldContain("Simpson");
    }
}