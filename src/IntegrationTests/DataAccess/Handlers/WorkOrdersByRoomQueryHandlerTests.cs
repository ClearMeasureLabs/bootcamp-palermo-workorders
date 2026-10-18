using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrdersByRoomQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnWorkOrdersForRoom_WhenWorkOrdersExistInTwoRooms()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("wobr_user", "First", "Last", "email@test.com");

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(new WorkOrder { Creator = creator, Number = "WOBR-1", RoomNumber = "CR-101" });
            context.Add(new WorkOrder { Creator = creator, Number = "WOBR-2", RoomNumber = "CR-101" });
            context.Add(new WorkOrder { Creator = creator, Number = "WOBR-3", RoomNumber = "CR-202" });
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkOrdersByRoomQueryHandler(dataContext);
        var result = await handler.Handle(new WorkOrdersByRoomQuery("CR-101"));

        result.Length.ShouldBe(2);
        result.Select(wo => wo.Number).ShouldBe(["WOBR-1", "WOBR-2"], ignoreOrder: true);
        result.ShouldAllBe(wo => wo.RoomNumber == "CR-101");
    }
}
