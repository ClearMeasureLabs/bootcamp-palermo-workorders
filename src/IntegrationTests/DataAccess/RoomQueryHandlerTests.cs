using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class RoomQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnAllRooms_OrderedByName()
    {
        new DatabaseTests().Clean();

        var roomC = new Room { Id = Guid.NewGuid(), Name = "Zephyr Room" };
        var roomA = new Room { Id = Guid.NewGuid(), Name = "Alpha Room" };
        var roomB = new Room { Id = Guid.NewGuid(), Name = "Beta Room" };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(roomC);
            context.Add(roomA);
            context.Add(roomB);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new RoomQueryHandler(dataContext);
        var rooms = await handler.Handle(new RoomGetAllQuery(), CancellationToken.None);

        rooms.Length.ShouldBe(3);
        rooms[0].Name.ShouldBe("Alpha Room");
        rooms[1].Name.ShouldBe("Beta Room");
        rooms[2].Name.ShouldBe("Zephyr Room");
    }
}
