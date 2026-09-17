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

        // Insert notes directly with explicit distinct timestamps so the ordering test
        // is deterministic regardless of the stubbed TimeProvider (which returns a fixed time).
        var olderTime = new DateTime(2000, 1, 1, 1, 0, 0, DateTimeKind.Utc);
        var newerTime = new DateTime(2000, 1, 1, 2, 0, 0, DateTimeKind.Utc);

        Guid note1Id, note2Id;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var note1 = new WorkOrderNote
            {
                WorkOrderId = workOrder.Id,
                AuthorId = author.Id,
                Text = "First note",
                CreatedAt = olderTime
            };
            var note2 = new WorkOrderNote
            {
                WorkOrderId = workOrder.Id,
                AuthorId = author.Id,
                Text = "Second note",
                CreatedAt = newerTime
            };
            context.Add(note1);
            context.Add(note2);
            await context.SaveChangesAsync();
            note1Id = note1.Id;
            note2Id = note2.Id;
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var notes = await bus.Send(new WorkOrderNotesQuery(workOrder.Id));

        notes.Length.ShouldBe(2);
        notes[0].Id.ShouldBe(note2Id);
        notes[1].Id.ShouldBe(note1Id);
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
