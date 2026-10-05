using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DemoData;
using ClearMeasure.Bootcamp.IntegrationTests.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.IntegrationTests;

[TestFixture]
public class ZDataLoader
{
    [Test]
    public void LoadData()
    {
        new DatabaseTests().Clean();
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            DemoEmployeeSeeder.Seed(context);
        }

        LoadChristmasConcertWorkOrders();
    }

    private static void LoadChristmasConcertWorkOrders()
    {
        using var db = TestHost.GetRequiredService<DbContext>();
        var revLovejoy = db.Set<Employee>().Single(e => e.UserName == "tlovejoy");
        var nedFlanders = db.Set<Employee>().Single(e => e.UserName == "nflanders");
        var maudeFlanders = db.Set<Employee>().Single(e => e.UserName == "mflanders");
        var groundskeeperWillie = db.Set<Employee>().Single(e => e.UserName == "gwillie");
        var organistEmployee = db.Set<Employee>().Single(e => e.UserName == "gfeesh");

        // Create Christmas Concert Work Orders
        var christmasOrder1 = new WorkOrder
        {
            Number = Guid.NewGuid().ToString().Substring(0, 5).ToUpper(),
            Creator = revLovejoy,
            Assignee = maudeFlanders,
            Status = WorkOrderStatus.Draft,
            Title = "Organize Christmas Concert Choir Practice Schedule",
            Description = "Coordinate weekly choir rehearsals for the Christmas concert. Schedule practice sessions for November and December leading up to the Christmas Eve service.",
            CreatedDate = new DateTime(2024, 10, 15, 9, 0, 0),
            RoomNumber = "Sanctuary"
        };
        db.Add(christmasOrder1);

        var christmasOrder2 = new WorkOrder
        {
            Number = Guid.NewGuid().ToString().Substring(0, 5).ToUpper(),
            Creator = revLovejoy,
            Assignee = groundskeeperWillie,
            Status = WorkOrderStatus.Assigned,
            Title = "Prepare Church Grounds for Christmas Decorations",
            Description = "Clean and prepare the church exterior and landscaping for Christmas decorations. Ensure proper lighting infrastructure and safe walkways for concert attendees.",
            CreatedDate = new DateTime(2024, 11, 1, 8, 0, 0),
            AssignedDate = new DateTime(2024, 11, 2, 10, 0, 0),
            RoomNumber = "Exterior Grounds"
        };
        db.Add(christmasOrder2);

        var christmasOrder3 = new WorkOrder
        {
            Number = Guid.NewGuid().ToString().Substring(0, 5).ToUpper(),
            Creator = nedFlanders,
            Assignee = organistEmployee,
            Status = WorkOrderStatus.InProgress,
            Title = "Tune and Maintain Church Organ for Christmas Concert",
            Description = "Perform complete maintenance and tuning of the church organ in preparation for Christmas concert performances. Test all stops and ensure optimal sound quality.",
            CreatedDate = new DateTime(2024, 11, 5, 14, 0, 0),
            AssignedDate = new DateTime(2024, 11, 6, 9, 0, 0),
            RoomNumber = "Sanctuary Organ Loft"
        };
        db.Add(christmasOrder3);

        var christmasOrder4 = new WorkOrder
        {
            Number = Guid.NewGuid().ToString().Substring(0, 5).ToUpper(),
            Creator = revLovejoy,
            Assignee = nedFlanders,
            Status = WorkOrderStatus.Draft,
            Title = "Setup Audio System for Christmas Concert",
            Description = "Configure and test the sanctuary sound system for the Christmas concert. Ensure microphones, speakers, and recording equipment are functioning properly.",
            CreatedDate = new DateTime(2024, 11, 10, 16, 0, 0),
            RoomNumber = "Sanctuary"
        };
        db.Add(christmasOrder4);

        var christmasOrder5 = new WorkOrder
        {
            Number = Guid.NewGuid().ToString().Substring(0, 5).ToUpper(),
            Creator = nedFlanders,
            Assignee = groundskeeperWillie,
            Status = WorkOrderStatus.Complete,
            Title = "Install Christmas Tree in Sanctuary",
            Description = "Select, transport, and install the Christmas tree in the sanctuary. Ensure proper placement and safety for the Christmas concert and services.",
            CreatedDate = new DateTime(2024, 12, 1, 10, 0, 0),
            AssignedDate = new DateTime(2024, 12, 1, 11, 0, 0),
            CompletedDate = new DateTime(2024, 12, 3, 15, 0, 0),
            RoomNumber = "Sanctuary"
        };
        db.Add(christmasOrder5);

        var christmasOrder6 = new WorkOrder
        {
            Number = Guid.NewGuid().ToString().Substring(0, 5).ToUpper(),
            Creator = revLovejoy,
            Assignee = maudeFlanders,
            Status = WorkOrderStatus.Assigned,
            Title = "Coordinate Christmas Concert Program Design",
            Description = "Design and prepare printed programs for the Christmas concert including song listings, performer credits, and special acknowledgments.",
            CreatedDate = new DateTime(2024, 11, 20, 13, 0, 0),
            AssignedDate = new DateTime(2024, 11, 21, 9, 0, 0),
            RoomNumber = "Church Office"
        };
        db.Add(christmasOrder6);
        db.SaveChanges();
    }
}
