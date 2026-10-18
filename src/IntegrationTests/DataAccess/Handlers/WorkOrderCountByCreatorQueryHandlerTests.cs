using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrderCountByCreatorQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnCountForRequestedCreator_WhenTwoCreatorsExist()
    {
        new DatabaseTests().Clean();

        var creatorA = new Employee("wocbc_a", "FirstA", "LastA", "a@test.com");
        var creatorB = new Employee("wocbc_b", "FirstB", "LastB", "b@test.com");

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creatorA);
            context.Add(creatorB);
            context.Add(new WorkOrder { Creator = creatorA, Number = "WOCBC1", Status = WorkOrderStatus.Draft });
            context.Add(new WorkOrder { Creator = creatorA, Number = "WOCBC2", Status = WorkOrderStatus.Draft });
            context.Add(new WorkOrder { Creator = creatorB, Number = "WOCBC3", Status = WorkOrderStatus.InProgress });
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkOrderCountByCreatorQueryHandler(dataContext);

        var countA = await handler.Handle(new WorkOrderCountByCreatorQuery("wocbc_a"));
        var countB = await handler.Handle(new WorkOrderCountByCreatorQuery("wocbc_b"));

        countA.ShouldBe(2);
        countB.ShouldBe(1);
    }
}
