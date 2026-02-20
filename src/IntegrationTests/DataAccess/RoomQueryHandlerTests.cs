using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class RoomQueryHandlerTests
{
    [Test]
    public async Task ShouldGetAllRooms()
    {
        new DatabaseTests().Clean();

        var choir = new Room { Name = "Choir" };
        var kitchen = new Room { Name = "Kitchen" };
        var chapel = new Room { Name = "Chapel" };
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(kitchen);
            context.Add(choir);
            context.Add(chapel);
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new RoomQueryHandler(dataContext);
        var rooms = await handler.Handle(new RoomGetAllQuery());

        Assert.That(rooms.Length, Is.EqualTo(3));
        Assert.That(rooms[0].Name, Is.EqualTo("Chapel"));
        Assert.That(rooms[1].Name, Is.EqualTo("Choir"));
        Assert.That(rooms[2].Name, Is.EqualTo("Kitchen"));
    }

    [Test]
    public async Task ShouldReturnEmptyArrayWhenNoRooms()
    {
        new DatabaseTests().Clean();

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new RoomQueryHandler(dataContext);
        var rooms = await handler.Handle(new RoomGetAllQuery());

        Assert.That(rooms.Length, Is.EqualTo(0));
    }
}
