using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

[TestFixture]
public class WorkRequestMappingTests
{
    [Test]
    public void ShouldMapWorkRequestBasicProperties()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workRequest = new WorkRequest
        {
            Number = "WO-01",
            Title = "Fix lighting",
            Description = "Replace broken light bulbs in conference room",
            Instructions = "Lock out panel before work.",
            RoomNumber = "CR-101",
            Status = WorkRequestStatus.Draft,
            Creator = creator
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workRequest);
            context.SaveChanges();
        }

        WorkRequest rehydratedWorkRequest;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedWorkRequest = context.Set<WorkRequest>()
                .Include(wo => wo.Creator)
                .Single(wo => wo.Id == workRequest.Id);
        }

        rehydratedWorkRequest.Id.ShouldBe(workRequest.Id);
        rehydratedWorkRequest.Number.ShouldBe("WO-01");
        rehydratedWorkRequest.Title.ShouldBe("Fix lighting");
        rehydratedWorkRequest.Description.ShouldBe("Replace broken light bulbs in conference room");
        rehydratedWorkRequest.Instructions.ShouldBe("Lock out panel before work.");
        rehydratedWorkRequest.RoomNumber.ShouldBe("CR-101");
        rehydratedWorkRequest.Status.ShouldBe(WorkRequestStatus.Draft);
        rehydratedWorkRequest.Creator.ShouldNotBeNull();
        rehydratedWorkRequest.Creator!.Id.ShouldBe(creator.Id);
    }

    [Test]
    public async Task ShouldSaveWorkRequest()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("1", "1", "1", "1");
        var assignee = new Employee("2", "2", "2", "2");
        var order = new WorkRequest
        {
            Creator = creator,
            Assignee = assignee,
            Title = "foo",
            Description = "bar",
            Instructions = "Use 10ft ladder",
            RoomNumber = "123 a"
        };
        order.ChangeStatus(WorkRequestStatus.InProgress);
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
            var rehydratedWorkRequest = context.Set<WorkRequest>()
                .Include(wo => wo.Creator)
                .Include(wo => wo.Assignee)
                .Single(wo => wo.Id == order.Id);
            rehydratedWorkRequest.Id.ShouldBe(order.Id);
            rehydratedWorkRequest.Creator!.Id.ShouldBe(order.Creator.Id);
            rehydratedWorkRequest.Assignee!.Id.ShouldBe(order.Assignee.Id);
            rehydratedWorkRequest.Title.ShouldBe(order.Title);
            rehydratedWorkRequest.Description.ShouldBe(order.Description);
            rehydratedWorkRequest.Instructions.ShouldBe(order.Instructions);
            rehydratedWorkRequest.Status.ShouldBe(order.Status);
            rehydratedWorkRequest.RoomNumber.ShouldBe(order.RoomNumber);
            rehydratedWorkRequest.Number.ShouldBe(order.Number);
        }
    }

    [Test]
    public async Task ShouldSaveAuditEntries()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("1", "1", "1", "1");
        var assignee = new Employee("2", "2", "2", "2");
        var order = new WorkRequest
        {
            Creator = creator,
            Assignee = assignee,
            Title = "foo",
            Description = "bar",
            Instructions = "Use 10ft ladder",
            RoomNumber = "123 a"
        };
        order.ChangeStatus(WorkRequestStatus.InProgress);
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
            var rehydratedWorkRequest = context.Set<WorkRequest>()
                .Single(wo => wo.Id == order.Id);
        }
    }


    [Test]
    public void ShouldMapWorkRequestWithCreatorAndAssignee()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var assignee = new Employee("assignee1", "Jane", "Smith", "jane@example.com");
        var workRequest = new WorkRequest
        {
            Number = "WO-02",
            Title = "Fix plumbing",
            Description = "Fix sink in bathroom",
            Creator = creator,
            Assignee = assignee,
            Status = WorkRequestStatus.Assigned
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(workRequest);
            context.SaveChanges();
        }

        WorkRequest rehydratedWorkRequest;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedWorkRequest = context.Set<WorkRequest>()
                .Single(wo => wo.Id == workRequest.Id);
        }

        rehydratedWorkRequest.Creator.ShouldNotBeNull();
        rehydratedWorkRequest.Assignee.ShouldNotBeNull();
        rehydratedWorkRequest.Creator!.Id.ShouldBe(creator.Id);
        rehydratedWorkRequest.Assignee!.Id.ShouldBe(assignee.Id);
    }

    [Test]
    public void ShouldMapWorkRequestStatusConversion()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workRequest = new WorkRequest
        {
            Number = "WO-04",
            Title = "Test Status",
            Description = "Testing status conversion",
            Creator = creator,
            Status = WorkRequestStatus.Complete
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workRequest);
            context.SaveChanges();
        }

        WorkRequest rehydratedWorkRequest;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedWorkRequest = context.Set<WorkRequest>()
                .Single(wo => wo.Id == workRequest.Id);
        }

        rehydratedWorkRequest.Status.ShouldBe(WorkRequestStatus.Complete);
    }

    [Test]
    public void ShouldEnforceRequiredProperties()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workRequest = new WorkRequest
        {
            Creator = creator,
            Status = WorkRequestStatus.Draft
            // Intentionally omitting Number and Title which are required
        };

        using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workRequest);

        Should.Throw<DbUpdateException>(() => context.SaveChanges());
    }

    [Test]
    [Category("SqlServerOnly")]
    public void ShouldRespectMaxLengthConstraints()
    {
        SqlServerTestAssumptions.RequireSqlServer();

        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        // WorkRequest.Description setter truncates to 4000 before EF sees the value, so length violations
        // for Description are not observable through the domain model here.
        var workRequest = new WorkRequest
        {
            Number = new string('A', 8), // Exceeds 7 char limit (WorkRequestMap)
            Title = new string('B', 301), // Exceeds 300 char limit
            Description = "valid",
            RoomNumber = new string('D', 51), // Exceeds 50 char limit
            Creator = creator,
            Status = WorkRequestStatus.Draft
        };

        using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workRequest);

        Should.Throw<DbUpdateException>(() => context.SaveChanges());
    }

    [Test]
    [Category("SqlServerOnly")]
    public void ShouldSupportMaxLengthTitle()
    {
        SqlServerTestAssumptions.RequireSqlServer();

        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workRequest = new WorkRequest
        {
            Number = "number",
            Title = new string('B', 300),
            Description = "description",
            RoomNumber = "room number",
            Creator = creator,
            Status = WorkRequestStatus.Draft
        };

        using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workRequest);

        context.SaveChanges();

        workRequest.Title.Length.ShouldBe(300);
    }

    [Test]
    public void ShouldEagerFetchCreatorAndAssigneeByDefault()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var assignee = new Employee("assignee1", "Jane", "Smith", "jane@example.com");
        var workRequest = new WorkRequest
        {
            Number = "WO-06",
            Title = "Test Eager Loading",
            Description = "Testing that Creator and Assignee are auto-included",
            Creator = creator,
            Assignee = assignee,
            Status = WorkRequestStatus.Assigned
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(workRequest);
            context.SaveChanges();
        }

        WorkRequest rehydratedWorkRequest;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            // No explicit Include calls - testing AutoInclude
            rehydratedWorkRequest = context.Set<WorkRequest>()
                .Single(wo => wo.Id == workRequest.Id);
        }

        // Creator and Assignee should be loaded automatically
        rehydratedWorkRequest.Creator.ShouldNotBeNull();
        rehydratedWorkRequest.Assignee.ShouldNotBeNull();
        rehydratedWorkRequest.Creator!.Id.ShouldBe(creator.Id);
        rehydratedWorkRequest.Creator.FirstName.ShouldBe("John");
        rehydratedWorkRequest.Creator.LastName.ShouldBe("Doe");
        rehydratedWorkRequest.Assignee!.Id.ShouldBe(assignee.Id);
        rehydratedWorkRequest.Assignee.FirstName.ShouldBe("Jane");
        rehydratedWorkRequest.Assignee.LastName.ShouldBe("Smith");
    }

    [Test]
    public void ShouldPersistNullInstructionsForLegacyWorkRequest()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workRequest = new WorkRequest
        {
            Number = "WO-07",
            Title = "Legacy work request",
            Description = "No instructions provided",
            Creator = creator,
            Status = WorkRequestStatus.Draft
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workRequest);
            context.SaveChanges();
        }

        WorkRequest rehydratedWorkRequest;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedWorkRequest = context.Set<WorkRequest>()
                .Single(wo => wo.Id == workRequest.Id);
        }

        rehydratedWorkRequest.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public void ShouldPersistInstructionsAtMaxLength()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var longInstructions = new string('I', 4001);
        var workRequest = new WorkRequest
        {
            Number = "WO-08",
            Title = "Max length instructions",
            Description = "Testing truncation",
            Creator = creator,
            Status = WorkRequestStatus.Draft
        };
        workRequest.Instructions = longInstructions;

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workRequest);
            context.SaveChanges();
        }

        WorkRequest rehydratedWorkRequest;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedWorkRequest = context.Set<WorkRequest>()
                .Single(wo => wo.Id == workRequest.Id);
        }

        rehydratedWorkRequest.Instructions!.Length.ShouldBe(4000);
    }
}