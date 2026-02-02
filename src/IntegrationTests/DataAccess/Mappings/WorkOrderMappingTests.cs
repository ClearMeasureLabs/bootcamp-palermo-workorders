using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
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
    public void ShouldRespectMaxLengthConstraints()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workOrder = new WorkOrder
        {
            Number = new string('A', 51), // Exceeds 50 char limit
            Title = new string('B', 201), // Exceeds 200 char limit
            Description = new string('C', 1001), // Exceeds 1000 char limit
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

    [Test]
    public void Should_MapAndPersistInstructionsField()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var instructions = "1. Turn off power\n2. Replace component\n3. Test functionality";
        var workOrder = new WorkOrder
        {
            Number = "WO-07",
            Title = "Test Instructions",
            Description = "Testing Instructions field mapping",
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

        WorkOrder rehydratedWorkOrder;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedWorkOrder = context.Set<WorkOrder>()
                .Single(wo => wo.Id == workOrder.Id);
        }

        rehydratedWorkOrder.Instructions.ShouldBe(instructions);
    }

    [Test]
    public void Should_PersistWorkOrderWithEmptyInstructions()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-08",
            Title = "Test Empty Instructions",
            Description = "Testing empty Instructions field",
            Instructions = string.Empty,
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

        rehydratedWorkOrder.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public void Should_PersistWorkOrderWithNullInstructions()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-09",
            Title = "Test Null Instructions",
            Description = "Testing null Instructions field",
            Instructions = null,
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

        rehydratedWorkOrder.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public void Should_Persist4000CharacterInstructions()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var instructions4000 = new string('x', 4000);
        var workOrder = new WorkOrder
        {
            Number = "WO-10",
            Title = "Test 4000 Char Instructions",
            Description = "Testing maximum length Instructions",
            Instructions = instructions4000,
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

        rehydratedWorkOrder.Instructions.ShouldBe(instructions4000);
        rehydratedWorkOrder.Instructions!.Length.ShouldBe(4000);
    }

    [Test]
    public async Task Should_UpdateInstructionsOnExistingWorkOrder()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var initialInstructions = "Initial instructions";
        var workOrder = new WorkOrder
        {
            Number = "WO-11",
            Title = "Test Instructions Update",
            Description = "Testing Instructions update",
            Instructions = initialInstructions,
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        var updatedInstructions = "Updated instructions with more details";
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var existingWorkOrder = context.Set<WorkOrder>()
                .Single(wo => wo.Id == workOrder.Id);
            existingWorkOrder.Instructions = updatedInstructions;
            await context.SaveChangesAsync();
        }

        WorkOrder rehydratedWorkOrder;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedWorkOrder = context.Set<WorkOrder>()
                .Single(wo => wo.Id == workOrder.Id);
        }

        rehydratedWorkOrder.Instructions.ShouldBe(updatedInstructions);
    }
}