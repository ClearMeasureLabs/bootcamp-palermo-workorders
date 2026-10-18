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
    public async Task ShouldReturnCountForCreator_WhenTwoCreatorsExist()
    {
        new DatabaseTests().Clean();

        var creator1 = new Employee("wocbc_a", "First", "Last", "a@test.com");
        var creator2 = new Employee("wocbc_b", "First", "Last", "b@test.com");

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator1);
            context.Add(creator2);
            context.Add(new WorkOrder { Creator = creator1, Number = "WOCBC1", Status = WorkOrderStatus.Draft });
            context.Add(new WorkOrder { Creator = creator1, Number = "WOCBC2", Status = WorkOrderStatus.Draft });
            context.Add(new WorkOrder { Creator = creator2, Number = "WOCBC3", Status = WorkOrderStatus.Draft });
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkOrderCountByCreatorQueryHandler(dataContext);

        var count1 = await handler.Handle(new WorkOrderCountByCreatorQuery("wocbc_a"));
        var count2 = await handler.Handle(new WorkOrderCountByCreatorQuery("wocbc_b"));

        count1.ShouldBe(2);
        count2.ShouldBe(1);
    }
}
