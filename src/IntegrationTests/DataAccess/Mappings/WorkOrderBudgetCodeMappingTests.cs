using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

[TestFixture]
public class WorkOrderBudgetCodeMappingTests
{
    [Test]
    public void ShouldPersistBudgetCode()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("budgetuser", "Budget", "User", "budget@example.com");
        var order = new WorkOrder
        {
            Number = "WO-BGT",
            Title = "Budget code order",
            Description = "Persists budget code",
            Creator = creator,
            BudgetCode = "BLDG-01"
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(order);
            context.SaveChanges();
        }

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydrated = context.Set<WorkOrder>().Single(w => w.Id == order.Id);
            rehydrated.BudgetCode.ShouldBe("BLDG-01");
        }
    }
}
