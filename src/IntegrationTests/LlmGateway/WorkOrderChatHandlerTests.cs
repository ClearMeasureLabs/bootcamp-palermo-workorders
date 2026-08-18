using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;

[TestFixture]
public class WorkOrderChatHandlerTests : LlmTestBase
{
    [Test]
    public async Task Should_ThrowOperationCanceledException_When_TokenIsAlreadyCanceled()
    {
        var workOrder = Faker<WorkOrder>();
        var handler = TestHost.GetRequiredService<WorkOrderChatHandler>();
        var query = new WorkOrderChatQuery("What is the number of this work order??", workOrder);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await handler.Handle(query, cts.Token));
    }

    [Test]
    public async Task Handle_WithValidWorkOrder_ReturnsChatResponse()
    {
        var workOrder = Faker<WorkOrder>();
        var handler = TestHost.GetRequiredService<WorkOrderChatHandler>();
        var query = new WorkOrderChatQuery("What is the number of this work order??", workOrder);

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

        if (!responseText!.Contains(workOrder.Number!, StringComparison.OrdinalIgnoreCase))
        {
            Assert.Inconclusive(
                $"LLM response did not contain work order number '{workOrder.Number}'");
        }
    }

    [Test]
    public async Task Handle_WithListEmployeesPrompt_ReturnsEmployeeData()
    {
        new ZDataLoader().LoadData();
        var workOrder = Faker<WorkOrder>();
        var handler = TestHost.GetRequiredService<WorkOrderChatHandler>();
        var query = new WorkOrderChatQuery("list all employees", workOrder);

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
