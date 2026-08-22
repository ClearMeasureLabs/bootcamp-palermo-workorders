using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class WorkOrderInstructionsPersistenceTests
{
    [Test]
    public void ShouldPersistAndReloadInstructions()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-IN1",
            Title = "Replace filter",
            Description = "HVAC filter replacement",
            Instructions = "Shut off unit before opening panel.",
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workOrder);
            context.SaveChanges();
        }

        WorkOrder rehydratedWorkOrder;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedWorkOrder = context.Set<WorkOrder>()
                .Single(wo => wo.Id == workOrder.Id);
        }

        rehydratedWorkOrder.Instructions.ShouldBe("Shut off unit before opening panel.");
    }

    [Test]
    public void ShouldPersistBlankInstructionsAsEmpty()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-IN2",
            Title = "Blank instructions",
            Description = "No guidance",
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };
        workOrder.Instructions = null;

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workOrder);
            context.SaveChanges();
        }

        WorkOrder rehydratedWorkOrder;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedWorkOrder = context.Set<WorkOrder>()
                .Single(wo => wo.Id == workOrder.Id);
        }

        rehydratedWorkOrder.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public void ShouldPersistInstructionsAtMaxLength()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var maxLengthInstructions = new string('I', WorkOrder.InstructionsMaxLength);
        var workOrder = new WorkOrder
        {
            Number = "WO-IN3",
            Title = "Max length instructions",
            Description = "Exactly 4000 characters",
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };
        workOrder.Instructions = maxLengthInstructions;

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workOrder);
            context.SaveChanges();
        }

        WorkOrder rehydratedWorkOrder;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedWorkOrder = context.Set<WorkOrder>()
                .Single(wo => wo.Id == workOrder.Id);
        }

        rehydratedWorkOrder.Instructions.ShouldBe(maxLengthInstructions);
        rehydratedWorkOrder.Instructions!.Length.ShouldBe(WorkOrder.InstructionsMaxLength);
    }
}
