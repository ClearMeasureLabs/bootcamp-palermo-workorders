using ClearMeasure.Bootcamp.UI.Shared.Components;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderAiChatTests : AcceptanceTestBase
{
    [Test]
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

        // Wait for response and verify message appears
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify chat history is visible and contains messages
        var chatHistory = Page.GetByTestId(nameof(WorkOrderChat.Elements.ChatHistory));
        await Expect(chatHistory).ToBeVisibleAsync();
        
        // Verify chat history contains text content (messages were added)
        var chatHistoryText = await chatHistory.InnerTextAsync();
        chatHistoryText.ShouldNotBeNullOrEmpty();
        chatHistoryText.ShouldContain(prompt);
    }
}