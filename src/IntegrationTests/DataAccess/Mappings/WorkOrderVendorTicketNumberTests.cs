using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

[TestFixture]
public class WorkOrderVendorTicketNumberTests
{
    [Test]
    public void ShouldPersistVendorTicketNumber()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("vendortkt", "Vendor", "Ticket", "vendortkt@example.com");
        var wo = new WorkOrder
        {
            Number = "VT-0001",
            Title = "Vendor ticket persist",
            Description = "Desc",
            Status = WorkOrderStatus.Draft,
            Creator = creator,
            VendorTicketNumber = "VND-12345"
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(wo);
            context.SaveChanges();
        }

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydrated = context.Set<WorkOrder>().Single(w => w.Id == wo.Id);
            rehydrated.VendorTicketNumber.ShouldBe("VND-12345");
        }
    }
}
