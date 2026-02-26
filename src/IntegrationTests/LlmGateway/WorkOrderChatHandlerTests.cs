using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;

[TestFixture]
public class WorkOrderChatHandlerTests : LlmTestBase
{
    [Test]
    public async Task Handle_WithValidWorkOrder_ReturnsChatResponse()
    {
        var workOrder = Faker<WorkOrder>();
        var handler = TestHost.GetRequiredService<WorkOrderChatHandler>();
        var query = new WorkOrderChatQuery("What is the title of this work order??", workOrder);

        ChatResponse response = await handler.Handle(query, CancellationToken.None);

        response.ShouldNotBeNull();
        response.Messages.ShouldNotBeEmpty();
        response.Messages.Last().Text.ShouldContain(workOrder.Title!);
    }

    [Test]
    public async Task Handle_WithListEmployeesPrompt_ReturnsEmployeeData()
    {
        new ZDataLoader().LoadData();
        var workOrder = Faker<WorkOrder>();
        var handler = TestHost.GetRequiredService<WorkOrderChatHandler>();
        var query = new WorkOrderChatQuery("list all employees", workOrder);

        ChatResponse response = await handler.Handle(query, CancellationToken.None);

        response.ShouldNotBeNull();
        response.Messages.ShouldNotBeEmpty();
        response.Messages.Last().Text.ShouldContain("Lovejoy");
    }
}
