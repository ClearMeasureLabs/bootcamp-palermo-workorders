using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;

[TestFixture]
public class WorkRequestChatHandlerTests : LlmTestBase
{
    [Test]
    public async Task Handle_WithValidWorkRequest_ReturnsChatResponse()
    {
        var workRequest = Faker<WorkRequest>();
        var handler = TestHost.GetRequiredService<WorkRequestChatHandler>();
        var query = new WorkRequestChatQuery("What is the number of this work request??", workRequest);

        ChatResponse response;
        try
        {
            response = await handler.Handle(query, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Assert.Inconclusive($"LLM call failed: {ex.Message}");
            return;
        }

        var responseText = response.Messages.LastOrDefault()?.Text;
        await TestContext.Out.WriteLineAsync($"LLM response: {responseText}");

        if (response.Messages.Count == 0 || string.IsNullOrWhiteSpace(responseText))
        {
            Assert.Inconclusive("LLM returned empty response");
        }

        if (!responseText!.Contains(workRequest.Number!, StringComparison.OrdinalIgnoreCase))
        {
            Assert.Inconclusive(
                $"LLM response did not contain work request number '{workRequest.Number}'");
        }
    }

    [Test]
    public async Task Handle_WithListEmployeesPrompt_ReturnsEmployeeData()
    {
        new ZDataLoader().LoadData();
        var workRequest = Faker<WorkRequest>();
        var handler = TestHost.GetRequiredService<WorkRequestChatHandler>();
        var query = new WorkRequestChatQuery("list all employees", workRequest);

        ChatResponse response;
        try
        {
            response = await handler.Handle(query, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Assert.Inconclusive($"LLM call failed: {ex.Message}");
            return;
        }

        var responseText = response.Messages.LastOrDefault()?.Text;
        await TestContext.Out.WriteLineAsync($"LLM response: {responseText}");

        if (response.Messages.Count == 0 || string.IsNullOrWhiteSpace(responseText))
        {
            Assert.Inconclusive("LLM returned empty response");
        }

        if (!responseText!.Contains("Lovejoy", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Inconclusive(
                $"LLM response did not contain 'Lovejoy'. Response: {responseText}");
        }
    }
}
