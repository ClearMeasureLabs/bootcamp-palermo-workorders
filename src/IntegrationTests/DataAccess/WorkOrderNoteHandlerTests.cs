using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class WorkOrderNoteHandlerTests : IntegratedTestBase
{
    [Test]
    public async Task AddWorkOrderNoteCommand_ShouldPersistNote()
    {
        new DatabaseTests().Clean();

        var author = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        var workOrder = new WorkOrder { Number = "WO-N01", Creator = author };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(author);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var command = new AddWorkOrderNoteCommand(workOrder, author, "Test note text");
        var note = await bus.Send(command);

        note.ShouldNotBeNull();
        note.Text.ShouldBe("Test note text");
        note.AuthorId.ShouldBe(author.Id);
        note.WorkOrderId.ShouldBe(workOrder.Id);
        note.CreatedAt.ShouldNotBe(default);

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var persisted = context.Set<WorkOrderNote>().SingleOrDefault(n => n.Id == note.Id);
            persisted.ShouldNotBeNull();
            persisted.Text.ShouldBe("Test note text");
        }
    }

    [Test]
    public async Task WorkOrderNotesQuery_ShouldReturnNotesForWorkOrder_NewestFirst()
    {
        new DatabaseTests().Clean();

        var author = new Employee("jpalermo2", "Jeffrey", "Palermo", "jp2@example.com");
        var workOrder = new WorkOrder { Number = "WO-N02", Creator = author };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(author);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var note1 = await bus.Send(new AddWorkOrderNoteCommand(workOrder, author, "First note"));
        await Task.Delay(10);
        var note2 = await bus.Send(new AddWorkOrderNoteCommand(workOrder, author, "Second note"));

        var notes = await bus.Send(new WorkOrderNotesQuery(workOrder.Id));

        notes.Length.ShouldBe(2);
        notes[0].Id.ShouldBe(note2.Id);
        notes[1].Id.ShouldBe(note1.Id);
        notes[0].Author.ShouldNotBeNull();
        notes[0].Author!.Id.ShouldBe(author.Id);
    }

    [Test]
    public async Task WorkOrderNotesQuery_ShouldReturnEmpty_WhenNoNotes()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("jpalermo3", "Jeffrey", "Palermo", "jp3@example.com");
        var workOrder = new WorkOrder { Number = "WO-N03", Creator = creator };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var notes = await bus.Send(new WorkOrderNotesQuery(workOrder.Id));

        notes.ShouldBeEmpty();
    }
}
