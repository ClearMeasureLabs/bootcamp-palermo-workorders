using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class RoomCommandHandlerTests
{
    [Test]
    public async Task ShouldPersistRoom_WhenSaveRoomCommandHandled()
    {
        new DatabaseTests().Clean();

        var room = new Room { Id = Guid.NewGuid(), Name = "Conference Room A" };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var handler = new RoomCommandHandler(context);
            await handler.Handle(new SaveRoomCommand(room), CancellationToken.None);
        }

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var persisted = context.Set<Room>().SingleOrDefault(r => r.Id == room.Id);
            persisted.ShouldNotBeNull();
            persisted.Name.ShouldBe("Conference Room A");
        }
    }

    [Test]
    public async Task ShouldUpdateRoom_WhenSaveRoomCommandHandledForExistingRoom()
    {
        new DatabaseTests().Clean();

        var room = new Room { Id = Guid.NewGuid(), Name = "Original Name" };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(room);
            await context.SaveChangesAsync();
        }

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var updated = new Room { Id = room.Id, Name = "Updated Name" };
            var handler = new RoomCommandHandler(context);
            await handler.Handle(new SaveRoomCommand(updated), CancellationToken.None);
        }

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var all = context.Set<Room>().Where(r => r.Id == room.Id).ToArray();
            all.Length.ShouldBe(1);
            all[0].Name.ShouldBe("Updated Name");
        }
    }

    [Test]
    public async Task ShouldDeleteRoom_WhenDeleteRoomCommandHandled()
    {
        new DatabaseTests().Clean();

        var room = new Room { Id = Guid.NewGuid(), Name = "Room to Delete" };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(room);
            await context.SaveChangesAsync();
        }

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var handler = new RoomCommandHandler(context);
            await handler.Handle(new DeleteRoomCommand(room.Id), CancellationToken.None);
        }

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Set<Room>().ShouldBeEmpty();
        }
    }
}
