using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

[TestFixture]
public class WorkOrderEstimatedHoursMappingTests
{
    [Test]
    public void ShouldPersistEstimatedHours()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("esthours", "Est", "Hours", "esthours@example.com");
        var order = new WorkOrder
        {
            Creator = creator,
            Number = "WO-EH1",
            Title = "Estimate paint",
            EstimatedHours = 12.5m
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(order);
            context.SaveChanges();
        }

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydrated = context.Set<WorkOrder>().Single(wo => wo.Id == order.Id);
            rehydrated.EstimatedHours.ShouldBe(12.5m);
        }
    }
}
