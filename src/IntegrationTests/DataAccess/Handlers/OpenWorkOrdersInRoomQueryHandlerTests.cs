using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class OpenWorkOrdersInRoomQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnOpenOrdersInSameRoom_ExcludingClosedAndOtherRoomsAndExcludedNumber()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("owoir_user", "First", "Last", "email@test.com");

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(new WorkOrder
            {
                Creator = creator,
                Number = "OWOIR1",
                RoomNumber = "101",
                Title = "Same room draft",
                Status = WorkOrderStatus.Draft
            });
            context.Add(new WorkOrder
            {
                Creator = creator,
                Number = "OWOIR2",
                RoomNumber = "101",
                Title = "Same room assigned",
                Status = WorkOrderStatus.Assigned
            });
            context.Add(new WorkOrder
            {
                Creator = creator,
                Number = "OWOIR3",
                RoomNumber = "101",
                Title = "Same room in progress",
                Status = WorkOrderStatus.InProgress
            });
            context.Add(new WorkOrder
            {
                Creator = creator,
                Number = "OWOIR4",
                RoomNumber = "101",
                Title = "Same room complete",
                Status = WorkOrderStatus.Complete
            });
            context.Add(new WorkOrder
            {
                Creator = creator,
                Number = "OWOIR5",
                RoomNumber = "202",
                Title = "Other room",
                Status = WorkOrderStatus.Draft
            });
            context.Add(new WorkOrder
            {
                Creator = creator,
                Number = "OWOIRX",
                RoomNumber = "101",
                Title = "Excluded",
                Status = WorkOrderStatus.Draft
            });
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new OpenWorkOrdersInRoomQueryHandler(dataContext);
        var result = await handler.Handle(new OpenWorkOrdersInRoomQuery("101", "OWOIRX"));

        result.Select(wo => wo.Number).OrderBy(n => n).ShouldBe(["OWOIR1", "OWOIR2", "OWOIR3"]);
    }
}
