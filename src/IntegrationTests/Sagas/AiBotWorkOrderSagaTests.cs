using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.Constants;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.IntegrationTests.DataAccess;
using ClearMeasure.Bootcamp.IntegrationTests.Handlers;
using ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Sagas;

[TestFixture]
public class AiBotWorkOrderSagaTests : LlmTestBase
{
    [Test]
    public async Task Handle_AssignedToBot_SagaCompletesAndUpdatesDescription()
    {
        new DatabaseTests().Clean();

        var lead = new Role("Facility Lead", true, false);
        var bot = new Role(Roles.Bot, false, true);
        var creator = new Employee("testcreator", "Test", "Creator", "creator@test.local");
        creator.AddRole(lead);
        var aibot = new Employee("aibot", "AI", Roles.Bot, "aibot@system.local");
        aibot.AddRole(bot);

        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.Creator = creator;
        workOrder.Assignee = aibot;
        workOrder.Description = "Original description";

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(lead);
            context.Add(bot);
            context.Add(creator);
            context.Add(aibot);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        var workOrderNumber = workOrder.Number!;
        var command = new DraftToAssignedCommand(workOrder, creator);
        var handler = TestHost.GetRequiredService<StateCommandHandler>();

        var signalTask = AiBotSagaSignal.WaitForCompletion(workOrderNumber, TimeSpan.FromSeconds(30));
        await handler.Handle(command);
        await signalTask;

        await using var verifyContext = TestHost.GetRequiredService<DbContext>();
        var completedOrder = verifyContext.Find<WorkOrder>(workOrder.Id);

        completedOrder.ShouldNotBeNull();
        completedOrder.Status.ShouldBe(WorkOrderStatus.Complete);
        completedOrder.Description.ShouldContain("AI Bot");
        completedOrder.Assignee!.UserName.ShouldBe(aibot.UserName);
        completedOrder.AssignedDate.ShouldNotBeNull();
        completedOrder.CompletedDate.ShouldNotBeNull();
    }
}
