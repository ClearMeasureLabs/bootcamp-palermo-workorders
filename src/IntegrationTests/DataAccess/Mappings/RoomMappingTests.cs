using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.IntegrationTests.DataAccess;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

[TestFixture]
public class RoomMappingTests
{
	[Test]
	public void ShouldMapRoomBasicProperties()
	{
		new DatabaseTests().Clean();

		var room = new Room("Chapel");

		using (var context = TestHost.GetRequiredService<DbContext>())
		{
			context.Add(room);
			context.SaveChanges();
		}

		Room rehydratedRoom;
		using (var context = TestHost.GetRequiredService<DbContext>())
		{
			rehydratedRoom = context.Set<Room>()
				.Single(r => r.Id == room.Id);
		}

		rehydratedRoom.Id.ShouldBe(room.Id);
		rehydratedRoom.Name.ShouldBe("Chapel");
	}

	[Test]
	public async Task ShouldSaveMultipleRooms()
	{
		new DatabaseTests().Clean();

		var chapel = new Room("Chapel");
		var kitchen = new Room("Kitchen");
		var nursery = new Room("Nursery");

		await using (var context = TestHost.GetRequiredService<DbContext>())
		{
			context.Add(chapel);
			context.Add(kitchen);
			context.Add(nursery);
			await context.SaveChangesAsync();
		}

		await using (var context = TestHost.GetRequiredService<DbContext>())
		{
			var rooms = await context.Set<Room>().ToArrayAsync();
			rooms.Length.ShouldBeGreaterThanOrEqualTo(3);
			rooms.Any(r => r.Name == "Chapel").ShouldBeTrue();
			rooms.Any(r => r.Name == "Kitchen").ShouldBeTrue();
			rooms.Any(r => r.Name == "Nursery").ShouldBeTrue();
		}
	}

	[Test]
	public void ShouldMapWorkOrderRoomsRelationship()
	{
		new DatabaseTests().Clean();

		var creator = new Employee("creator1", "John", "Doe", "john@example.com");
		var chapel = new Room("Chapel");
		var kitchen = new Room("Kitchen");
		var workOrder = new WorkOrder
		{
			Number = "WO-001",
			Title = "Multi-room maintenance",
			Description = "Maintenance across multiple rooms",
			Status = WorkOrderStatus.Draft,
			Creator = creator
		};
		workOrder.Rooms.Add(chapel);
		workOrder.Rooms.Add(kitchen);

		using (var context = TestHost.GetRequiredService<DbContext>())
		{
			context.Add(creator);
			context.Add(chapel);
			context.Add(kitchen);
			context.Add(workOrder);
			context.SaveChanges();
		}

		WorkOrder rehydratedWorkOrder;
		using (var context = TestHost.GetRequiredService<DbContext>())
		{
			rehydratedWorkOrder = context.Set<WorkOrder>()
				.Include(wo => wo.Rooms)
				.Single(wo => wo.Id == workOrder.Id);
		}

		rehydratedWorkOrder.Rooms.Count.ShouldBe(2);
		rehydratedWorkOrder.Rooms.Any(r => r.Name == "Chapel").ShouldBeTrue();
		rehydratedWorkOrder.Rooms.Any(r => r.Name == "Kitchen").ShouldBeTrue();
	}

	[Test]
	public async Task ShouldCascadeDeleteWorkOrderRoomsWhenWorkOrderIsDeleted()
	{
		new DatabaseTests().Clean();

		var creator = new Employee("creator1", "John", "Doe", "john@example.com");
		var chapel = new Room("Chapel");
		var workOrder = new WorkOrder
		{
			Number = "WO-002",
			Title = "Delete test",
			Description = "Testing cascade delete",
			Status = WorkOrderStatus.Draft,
			Creator = creator
		};
		workOrder.Rooms.Add(chapel);

		await using (var context = TestHost.GetRequiredService<DbContext>())
		{
			context.Add(creator);
			context.Add(chapel);
			context.Add(workOrder);
			await context.SaveChangesAsync();
		}

		var workOrderId = workOrder.Id;
		var roomId = chapel.Id;

		// Delete the work order
		await using (var context = TestHost.GetRequiredService<DbContext>())
		{
			var woToDelete = context.Set<WorkOrder>().Find(workOrderId);
			context.Remove(woToDelete!);
			await context.SaveChangesAsync();
		}

		// Verify room still exists but junction is gone
		await using (var context = TestHost.GetRequiredService<DbContext>())
		{
			var room = context.Set<Room>().Find(roomId);
			room.ShouldNotBeNull();
			
			var workOrderStillExists = context.Set<WorkOrder>().Any(wo => wo.Id == workOrderId);
			workOrderStillExists.ShouldBeFalse();
		}
	}

	[Test]
	public void ShouldEnforceRequiredNameProperty()
	{
		new DatabaseTests().Clean();

		var room = new Room { Name = null! };

		using var context = TestHost.GetRequiredService<DbContext>();
		context.Add(room);

		Should.Throw<DbUpdateException>(() => context.SaveChanges());
	}

	[Test]
	public void ShouldEnforceMaxLengthOnName()
	{
		new DatabaseTests().Clean();

		var room = new Room(new string('A', 51)); // Exceeds 50 char limit

		using var context = TestHost.GetRequiredService<DbContext>();
		context.Add(room);

		Should.Throw<DbUpdateException>(() => context.SaveChanges());
	}
}
