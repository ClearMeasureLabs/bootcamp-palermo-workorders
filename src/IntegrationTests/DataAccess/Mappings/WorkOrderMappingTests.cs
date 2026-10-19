using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

[TestFixture]
public class WorkOrderMappingTests
{
    [Test]
    public void ShouldPersistCompletionNotes()
    {
        new DatabaseTests().Clean();

        var order = new WorkOrder
        {
            Number = "WO-CN1",
            Title = "Completion notes test",
            Description = "Desc",
            CompletionNotes = "Replaced the filter and tested airflow."
        };

        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        using (var context = new DataContext(new TestDatabaseConfiguration(configuration)))
        {
            context.Add(order);
            context.SaveChanges();
        }

        using (var context = new DataContext(new TestDatabaseConfiguration(configuration)))
        {
            var rehydrated = context.Set<WorkOrder>().Single(wo => wo.Id == order.Id);
            rehydrated.CompletionNotes.ShouldBe("Replaced the filter and tested airflow.");
        }
    }
}
