using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.IntegrationTests;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

[TestFixture]
public class WorkOrderMappingTests
{
    [Test]
    public void ShouldMapWorkOrderBasicProperties()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-01",
            Title = "Fix lighting",
            Description = "Replace broken light bulbs in conference room",
            Instructions = "Turn off breaker before replacing bulbs.",
            RoomNumber = "CR-101",
            Status = WorkOrderStatus.Draft,
            Creator = creator
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
                .Include(wo => wo.Creator)
                .Single(wo => wo.Id == workOrder.Id);
        }

        rehydratedWorkOrder.Id.ShouldBe(workOrder.Id);
        rehydratedWorkOrder.Number.ShouldBe("WO-01");
        rehydratedWorkOrder.Title.ShouldBe("Fix lighting");
        rehydratedWorkOrder.Description.ShouldBe("Replace broken light bulbs in conference room");
        rehydratedWorkOrder.Instructions.ShouldBe("Turn off breaker before replacing bulbs.");
        rehydratedWorkOrder.RoomNumber.ShouldBe("CR-101");
        rehydratedWorkOrder.Status.ShouldBe(WorkOrderStatus.Draft);
        rehydratedWorkOrder.Creator.ShouldNotBeNull();
        rehydratedWorkOrder.Creator!.Id.ShouldBe(creator.Id);
    }

    [Test]
    public async Task ShouldSaveWorkOrder()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("1", "1", "1", "1");
        var assignee = new Employee("2", "2", "2", "2");
        var order = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Title = "foo",
            Description = "bar",
            Instructions = "use ladder",
            RoomNumber = "123 a"
        };
        order.ChangeStatus(WorkOrderStatus.InProgress);
        order.Number = "123";

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        dataContext.Attach(order);
        await dataContext.SaveChangesAsync();

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydratedWorkOrder = context.Set<WorkOrder>()
                .Include(wo => wo.Creator)
                .Include(wo => wo.Assignee)
                .Single(wo => wo.Id == order.Id);
            rehydratedWorkOrder.Id.ShouldBe(order.Id);
            rehydratedWorkOrder.Creator!.Id.ShouldBe(order.Creator.Id);
            rehydratedWorkOrder.Assignee!.Id.ShouldBe(order.Assignee.Id);
            rehydratedWorkOrder.Title.ShouldBe(order.Title);
            rehydratedWorkOrder.Description.ShouldBe(order.Description);
            rehydratedWorkOrder.Instructions.ShouldBe(order.Instructions);
            rehydratedWorkOrder.Status.ShouldBe(order.Status);
            rehydratedWorkOrder.RoomNumber.ShouldBe(order.RoomNumber);
            rehydratedWorkOrder.Number.ShouldBe(order.Number);
        }
    }

    [Test]
    public async Task ShouldSaveAuditEntries()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("1", "1", "1", "1");
        var assignee = new Employee("2", "2", "2", "2");
        var order = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Title = "foo",
            Description = "bar",
            Instructions = "",
            RoomNumber = "123 a"
        };
        order.ChangeStatus(WorkOrderStatus.InProgress);
        order.Number = "123";

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        dataContext.Attach(order);
        await dataContext.SaveChangesAsync();

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydratedWorkOrder = context.Set<WorkOrder>()
                .Single(wo => wo.Id == order.Id);
        }
    }


    [Test]
    public void ShouldMapWorkOrderWithCreatorAndAssignee()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var assignee = new Employee("assignee1", "Jane", "Smith", "jane@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-02",
            Title = "Fix plumbing",
            Description = "Fix sink in bathroom",
            Creator = creator,
            Assignee = assignee,
            Status = WorkOrderStatus.Assigned
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(workOrder);
            context.SaveChanges();
        }

        WorkOrder rehydratedWorkOrder;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedWorkOrder = context.Set<WorkOrder>()
                .Single(wo => wo.Id == workOrder.Id);
        }

        rehydratedWorkOrder.Creator.ShouldNotBeNull();
        rehydratedWorkOrder.Assignee.ShouldNotBeNull();
        rehydratedWorkOrder.Creator!.Id.ShouldBe(creator.Id);
        rehydratedWorkOrder.Assignee!.Id.ShouldBe(assignee.Id);
    }

    [Test]
    public void ShouldMapWorkOrderStatusConversion()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-04",
            Title = "Test Status",
            Description = "Testing status conversion",
            Creator = creator,
            Status = WorkOrderStatus.Complete
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

        rehydratedWorkOrder.Status.ShouldBe(WorkOrderStatus.Complete);
    }

    [Test]
    public async Task ShouldPersistInstructionsUpTo4000Characters()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var instructions = new string('z', 4000);
        var workOrder = new WorkOrder
        {
            Number = "WO-INS",
            Title = "With instructions",
            Description = "desc",
            Instructions = instructions,
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var loaded = await context.Set<WorkOrder>().SingleAsync(wo => wo.Number == "WO-INS");
            loaded.Instructions.ShouldBe(instructions);
            loaded.Instructions!.Length.ShouldBe(4000);
        }
    }

    [Test]
    public void ShouldEnforceRequiredProperties()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Status = WorkOrderStatus.Draft
            // Intentionally omitting Number and Title which are required
        };

        using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);

        Should.Throw<DbUpdateException>(() => context.SaveChanges());
    }

    [Test]
    [Category("SqlServerOnly")]
    public void ShouldRespectMaxLengthConstraints()
    {
        SqlServerTestAssumptions.RequireSqlServer();

        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        // WorkOrder.Description and Instructions setters truncate to 4000 before EF sees the value, so length violations
        // for those fields are not observable through the domain model here.
        var workOrder = new WorkOrder
        {
            Number = new string('A', 8), // Exceeds 7 char limit (WorkOrderMap)
            Title = new string('B', 301), // Exceeds 300 char limit
            Description = "valid",
            // Instructions setter truncates to 4000 before EF (same as Description)
            Instructions = new string('E', 4001),
            RoomNumber = new string('D', 51), // Exceeds 50 char limit
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };

        using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);

        Should.Throw<DbUpdateException>(() => context.SaveChanges());
    }

    [Test]
    public void ShouldPersistInstructionsAtMaxLength()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var instructions = new string('i', 4000);
        var workOrder = new WorkOrder
        {
            Number = "WO-MAX",
            Title = "With instructions",
            Description = "desc",
            Instructions = instructions,
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workOrder);
            context.SaveChanges();
        }

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydrated = context.Set<WorkOrder>().Single(wo => wo.Id == workOrder.Id);
            rehydrated.Instructions.ShouldBe(instructions);
            rehydrated.Instructions!.Length.ShouldBe(4000);
        }
    }

    [Test]
    [Category("SqlServerOnly")]
    public void ShouldSupportMaxLengthTitle()
    {
        SqlServerTestAssumptions.RequireSqlServer();

        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workOrder = new WorkOrder
        {
            Number = "number",
            Title = new string('B', 300),
            Description = "description",
            RoomNumber = "room number",
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };

        using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);

        context.SaveChanges();

        workOrder.Title.Length.ShouldBe(300);
    }

    [Test]
    public void ShouldEagerFetchCreatorAndAssigneeByDefault()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var assignee = new Employee("assignee1", "Jane", "Smith", "jane@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-06",
            Title = "Test Eager Loading",
            Description = "Testing that Creator and Assignee are auto-included",
            Creator = creator,
            Assignee = assignee,
            Status = WorkOrderStatus.Assigned
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(workOrder);
            context.SaveChanges();
        }

        WorkOrder rehydratedWorkOrder;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            // No explicit Include calls - testing AutoInclude
            rehydratedWorkOrder = context.Set<WorkOrder>()
                .Single(wo => wo.Id == workOrder.Id);
        }

        // Creator and Assignee should be loaded automatically
        rehydratedWorkOrder.Creator.ShouldNotBeNull();
        rehydratedWorkOrder.Assignee.ShouldNotBeNull();
        rehydratedWorkOrder.Creator!.Id.ShouldBe(creator.Id);
        rehydratedWorkOrder.Creator.FirstName.ShouldBe("John");
        rehydratedWorkOrder.Creator.LastName.ShouldBe("Doe");
        rehydratedWorkOrder.Assignee!.Id.ShouldBe(assignee.Id);
        rehydratedWorkOrder.Assignee.FirstName.ShouldBe("Jane");
        rehydratedWorkOrder.Assignee.LastName.ShouldBe("Smith");
    }
}