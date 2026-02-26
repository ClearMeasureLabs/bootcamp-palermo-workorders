using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.LlmGateway;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.AIAgents;

[TestFixture]
public class AgentChatListWorkOrdersByCreatorTests
{
    /// <summary>
    /// Full round trip: real ChatClientFactory, real IChatClient (Ollama or Azure OpenAI).
    /// Requires an LLM to be configured and available. Loads ZDataLoader data so tlovejoy has 4 work orders.
    /// </summary>
    [Test]
    [CancelAfter(90_000)]
    public async Task Should_return_work_orders_created_by_tlovejoy_when_prompt_is_list_work_orders_I_created()
    {
        new ZDataLoader().LoadData();

        var bus = TestHost.GetRequiredService<IBus>();
        var query = new AgentChatQuery("list the work orders I created", "tlovejoy");
        var response = await bus.Send(query);

        response.ShouldNotBeNull();
        response.Text.ShouldNotBeNullOrWhiteSpace();

        response.Text.ShouldContain("Organize Christmas Concert Choir Practice Schedule");
        response.Text.ShouldContain("Prepare Church Grounds for Christmas Decorations");
        response.Text.ShouldContain("Setup Audio System for Christmas Concert");
        response.Text.ShouldContain("Coordinate Christmas Concert Program Design");
    }
}
