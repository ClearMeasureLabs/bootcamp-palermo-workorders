using ClearMeasure.Bootcamp.LlmGateway;
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

        messages.Count.ShouldBe(4);
        messages[0].Role.ShouldBe(ChatRole.System);
        messages[1].Role.ShouldBe(ChatRole.System);
        messages[2].Role.ShouldBe(ChatRole.System);
        messages[2].Text.ShouldContain("tlovejoy");
        messages[3].Role.ShouldBe(ChatRole.User);
        messages[3].Text.ShouldBe("List open work orders");
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

        messages.Count.ShouldBe(7);
        messages[3].Role.ShouldBe(ChatRole.User);
        messages[3].Text.ShouldBe("hello");
        messages[4].Role.ShouldBe(ChatRole.Assistant);
        messages[4].Text.ShouldBe("hi there");
        messages[5].Role.ShouldBe(ChatRole.Assistant);
        messages[5].Text.ShouldBe("treated as assistant");
        messages[6].Role.ShouldBe(ChatRole.User);
        messages[6].Text.ShouldBe("follow up");
    }
}
