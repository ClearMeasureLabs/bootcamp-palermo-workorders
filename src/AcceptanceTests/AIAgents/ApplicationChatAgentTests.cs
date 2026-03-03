using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.IntegrationTests;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using NavMenu = ClearMeasure.Bootcamp.UI.Shared.NavMenu;

namespace ClearMeasure.Bootcamp.AcceptanceTests.AIAgents;

/// <summary>
///     Acceptance test for the AI Agent page (/ai-agent).
///     Sends a natural-language prompt through the Blazor UI that instructs the LLM
///     to create and assign a work order, then verifies the database reflects the changes.
/// </summary>
[TestFixture]
public class ApplicationChatAgentTests : AcceptanceTestBase
{
    [SetUp]
    public async Task EnsureLlmAvailable()
    {
        await SkipIfNoChatClient();
    }

    [Test, Retry(2)]
    public async Task ShouldCreateWorkOrderViaAiAgentChat()
    {
        await LoginAsCurrentUser();

        // Navigate to the AI Agent page
        await Click(nameof(NavMenu.Elements.AiAgent));
        await Page.WaitForURLAsync("**/ai-agent");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Send a prompt that instructs the AI to create a work order and assign it
        const string prompt =
            "I am Timothy Lovejoy (username tlovejoy). " +
            "Create a work order for Groundskeeper Willie (username gwillie) to mow the grass. " +
            "He should take care to edge around the prayer garden. " +
            "Use 'tlovejoy' as the creatorUsername. " +
            "After creating it, assign it to gwillie using the DraftToAssignedCommand " +
            "with executingUsername='tlovejoy' and assigneeUsername='gwillie'.";

        await Input(nameof(ApplicationChat.Elements.ChatInput), prompt);
        await Click(nameof(ApplicationChat.Elements.SendButton));

        // Wait for the AI response -- the LLM needs to invoke create-work-order
        // then execute-work-order-command, so allow a generous timeout
        var aiMessage = Page.GetByTestId(nameof(ApplicationChat.Elements.AiMessage) + "1");
        await aiMessage.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });

        // Verify chat history contains a response
        var chatHistory = Page.GetByTestId(nameof(ApplicationChat.Elements.ChatHistory));
        await Expect(chatHistory).ToBeVisibleAsync();
        var chatText = await chatHistory.InnerTextAsync();
        chatText.ShouldNotBeNullOrEmpty();

        // Query the database to verify the work order was created and assigned
        var bus = TestHost.GetRequiredService<IBus>();
        var workOrders = await bus.Send(new WorkOrderSpecificationQuery());

        var createdWo = workOrders.FirstOrDefault(wo =>
            wo.Assignee?.UserName == "gwillie" &&
            wo.Creator?.UserName == "tlovejoy" &&
            (wo.Title!.Contains("grass", StringComparison.OrdinalIgnoreCase) ||
             wo.Title!.Contains("mow", StringComparison.OrdinalIgnoreCase) ||
             wo.Description!.Contains("grass", StringComparison.OrdinalIgnoreCase)));

        createdWo.ShouldNotBeNull(
            "Expected a work order about mowing grass created by tlovejoy and assigned to gwillie");
        createdWo.Creator!.UserName.ShouldBe("tlovejoy");
        createdWo.Assignee!.UserName.ShouldBe("gwillie");
        createdWo.Status.ShouldBe(WorkOrderStatus.Assigned);
        createdWo.Description.ShouldNotBeNullOrEmpty();

        var description = createdWo.Description!.ToLowerInvariant();
        (description.Contains("edge") || description.Contains("edging") || description.Contains("prayer garden"))
            .ShouldBeTrue(
                $"Expected description to mention edging or prayer garden: {createdWo.Description}");
    }
}
