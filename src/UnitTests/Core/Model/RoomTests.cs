using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class RoomTests
{
	[Test]
	public void ShouldCreateRoom_WithName()
	{
		var room = new Room("Chapel");

		room.Name.ShouldBe("Chapel");
		room.Id.ShouldBe(Guid.Empty);
	}

	[Test]
	public void ShouldCreateRoom_WithDefaultConstructor()
	{
		var room = new Room();

		room.Name.ShouldNotBeNull();
		room.Id.ShouldBe(Guid.Empty);
	}

	[Test]
	public void ShouldImplementEquals_BasedOnId()
	{
		var id = Guid.NewGuid();
		var room1 = new Room("Chapel") { Id = id };
		var room2 = new Room("Chapel") { Id = id };
		var room3 = new Room("Kitchen") { Id = Guid.NewGuid() };

		room1.Equals(room2).ShouldBeTrue();
		room1.Equals(room3).ShouldBeFalse();
	}

	[Test]
	public void ShouldProvideStringRepresentation()
	{
		var room = new Room("Nursery");

		room.ToString().ShouldBe("Nursery");
	}
}
