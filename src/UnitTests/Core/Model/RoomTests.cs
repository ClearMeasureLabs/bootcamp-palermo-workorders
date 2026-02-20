using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class RoomTests
{
    [Test]
    public void PropertiesShouldInitializeProperly()
    {
        var room = new Room();
        Assert.That(room.Id, Is.EqualTo(Guid.Empty));
        Assert.That(room.Name, Is.EqualTo(string.Empty));
    }

    [Test]
    public void PropertiesShouldGetAndSetProperly()
    {
        var room = new Room();
        var guid = Guid.NewGuid();

        room.Id = guid;
        room.Name = "Chapel";

        Assert.That(room.Id, Is.EqualTo(guid));
        Assert.That(room.Name, Is.EqualTo("Chapel"));
    }

    [Test]
    public void ToStringShouldReturnName()
    {
        var room = new Room { Name = "Kitchen" };
        Assert.That(room.ToString(), Is.EqualTo("Kitchen"));
    }

    [Test]
    public void ShouldImplementEquality()
    {
        var room1 = new Room();
        var room2 = new Room();

        room1.ShouldNotBe(room2);
        room2.ShouldNotBe(room1);
        room1.Id = Guid.NewGuid();
        room2.Id = room1.Id;
        room1.ShouldBe(room2);
        room2.ShouldBe(room1);
        (room1 == room2).ShouldBeTrue();
    }
}
