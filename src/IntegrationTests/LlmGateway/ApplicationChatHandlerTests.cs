using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using NUnit.Framework.Internal;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;

[TestFixture]
public class ApplicationChatHandlerTests : LlmTestBase
{
    [Test]
    public async Task Handle_AskForWorkOrdersICreated_ReturnsWorkOrderData()
    {
        new ZDataLoader().LoadData();
        var handler = TestHost.GetRequiredService<ApplicationChatHandler>();
        var currentUser = "tlovejoy";
        var query = new ApplicationChatQuery("Show me all the work orders that I created", currentUser);

        ChatResponse response = await handler.Handle(query, CancellationToken.None);

        response.ShouldNotBeNull();
        response.Messages.ShouldNotBeEmpty();
        response.Messages.Last().Text.ShouldNotBeNullOrWhiteSpace();
        await TestContext.Out.WriteLineAsync(response.Messages.Last().Text!);
    }

    [Test]
    public async Task Handle_CreateAndAssignWorkOrder_ForGwillie()
    {
        new ZDataLoader().LoadData();
        var handler = TestHost.GetRequiredService<ApplicationChatHandler>(newScope: true);
        var currentUser = "tlovejoy";
        var prompt = "have groundskeeper willie mow the grass. Yes, assign the new work order. confirmed. only " +
                     "return the work order number";
        var query = new ApplicationChatQuery(prompt, currentUser);

        ChatResponse response = await handler.Handle(query, CancellationToken.None);

        var db = TestHost.GetRequiredService<DataContext>();

        var workOrder = await db.Set<WorkOrder>()
            .Include(x => x.Assignee)
            .SingleOrDefaultAsync(wo => wo.Number == response.Messages.Last().Text);

        workOrder.ShouldNotBeNull();
        workOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
        workOrder.Assignee.ShouldNotBeNull();
        workOrder.Assignee.UserName.ShouldBe("gwillie");
    }
}