using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Microsoft.Extensions.AI;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class ApplicationChatHandlerTests
{
    [Test]
    public void BuildChatMessages_WhenHistoryEmpty_IncludesSystemAndUserPrompt()
    {
        var query = new ApplicationChatQuery("List open work orders", "tlovejoy");

        var messages = ApplicationChatHandler.BuildChatMessages(query);

        messages.Count.ShouldBe(5);
        messages[0].Role.ShouldBe(ChatRole.System);
        messages[1].Role.ShouldBe(ChatRole.System);
        messages[2].Role.ShouldBe(ChatRole.System);
        messages[3].Role.ShouldBe(ChatRole.System);
        messages[3].Text.ShouldContain("tlovejoy");
        messages[4].Role.ShouldBe(ChatRole.User);
        messages[4].Text.ShouldBe("List open work orders");
    }

    [Test]
    public void BuildChatMessages_WhenHistoryHasUserAndAssistant_MapsRoles()
    {
        var query = new ApplicationChatQuery("follow up", "gwillie")
        {
            ChatHistory =
            [
                new ChatHistoryMessage("user", "hello"),
                new ChatHistoryMessage("assistant", "hi there"),
                new ChatHistoryMessage("other", "treated as assistant")
            ]
        };

        var messages = ApplicationChatHandler.BuildChatMessages(query);

        messages.Count.ShouldBe(8);
        messages[4].Role.ShouldBe(ChatRole.User);
        messages[4].Text.ShouldBe("hello");
        messages[5].Role.ShouldBe(ChatRole.Assistant);
        messages[5].Text.ShouldBe("hi there");
        messages[6].Role.ShouldBe(ChatRole.Assistant);
        messages[6].Text.ShouldBe("treated as assistant");
        messages[7].Role.ShouldBe(ChatRole.User);
        messages[7].Text.ShouldBe("follow up");
    }

    [Test]
    public void ToResult_WhenTextEmptyButMessagesHaveAssistant_ReturnsTextFromMessages()
    {
        var response = new ChatResponse(
        [
            new ChatMessage(ChatRole.Assistant, ""),
            new ChatMessage(ChatRole.Assistant, "WO-1 due 2026-08-29")
        ]);

        var result = ApplicationChatHandler.ToResult(response);

        result.Text.ShouldBe("WO-1 due 2026-08-29");
    }

    [Test]
    public void ToResult_WhenTextPresent_ReturnsText()
    {
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "already set")]);

        var result = ApplicationChatHandler.ToResult(response);

        result.Text.ShouldBe("already set");
    }

    [Test]
    public void ApplicationChatResult_ShouldRoundTripThroughWebServiceMessage()
    {
        var dto = new ApplicationChatResult("WO-1 due 2026-08-29\nWO-2 due 2026-09-05");
        var rehydrated = RemotableRequestTests.SimulateRemoteObject(dto);
        rehydrated.Text.ShouldBe(dto.Text);
    }
}
