using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class WorkRequestQueryHandlerTests
{
    [Test]
    public async Task ShouldGetWorkRequestByNumber()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("1", "1", "1", "1");
        var order1 = new WorkRequest();
        order1.Creator = creator;
        order1.Number = "123";
        var order2 = new WorkRequest();
        order2.Creator = creator;
        order2.Number = "456";

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(order1);
            context.Add(order2);
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkRequestQueryHandler(dataContext);
        var order123 = (await repository.GetWorkRequestAsync("123"))!;
        var order456 = (await repository.GetWorkRequestAsync("456"))!;

        order123.Id.ShouldBe(order1.Id);
        order456.Id.ShouldBe(order2.Id);
    }


    [Test]
    public async Task ShouldSearchBySpecificationWithAssignee()
    {
        new DatabaseTests().Clean();

        var employee1 = new Employee("1", "1", "1", "1");
        var employee2 = new Employee("2", "2", "2", "2");
        var order1 = new WorkRequest();
        order1.Creator = employee2;
        order1.Assignee = employee1;
        order1.Number = "123";
        var order2 = new WorkRequest();
        order2.Creator = employee1;
        order2.Assignee = employee2;
        order2.Number = "456";

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee1);
            context.Add(employee2);
            context.Add(order1);
            context.Add(order2);
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkRequestQueryHandler(dataContext);
        var specification = new WorkRequestSearchSpecification();
        specification.MatchAssignee(employee1);
        var orders = await repository.GetWorkRequestsAsync(specification);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithCreator()
    {
        new DatabaseTests().Clean();

        var creator1 = new Employee("1", "1", "1", "1");
        var creator2 = new Employee("2", "2", "2", "2");
        var order1 = new WorkRequest();
        order1.Creator = creator1;
        order1.Number = "123";
        var order2 = new WorkRequest();
        order2.Creator = creator2;
        order2.Number = "456";

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator1);
            context.Add(creator2);
            context.Add(order1);
            context.Add(order2);
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkRequestQueryHandler(dataContext);
        var specification = new WorkRequestSearchSpecification();
        specification.MatchCreator(creator1);
        var orders = await repository.GetWorkRequestsAsync(specification);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithFullSpecification()
    {
        new DatabaseTests().Clean();

        var employee1 = new Employee("1", "1", "1", "1");
        var employee2 = new Employee("2", "2", "2", "2");
        var order1 = new WorkRequest();
        order1.Creator = employee2;
        order1.Assignee = employee1;
        order1.Number = "123";
        order1.Status = WorkRequestStatus.Assigned;
        var order2 = new WorkRequest();
        order2.Creator = employee1;
        order2.Assignee = employee2;
        order2.Number = "456";
        order2.Status = WorkRequestStatus.Draft;

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee1);
            context.Add(employee2);
            context.Add(order1);
            context.Add(order2);
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkRequestQueryHandler(dataContext);
        var specification = new WorkRequestSearchSpecification();
        specification.MatchStatus(WorkRequestStatus.Assigned);
        specification.MatchCreator(employee2);
        specification.MatchAssignee(employee1);
        var orders = await repository.GetWorkRequestsAsync(specification);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithStatus()
    {
        new DatabaseTests().Clean();

        var employee1 = new Employee("1", "1", "1", "1");
        var employee2 = new Employee("2", "2", "2", "2");
        var order1 = new WorkRequest();
        order1.Creator = employee2;
        order1.Assignee = employee1;
        order1.Number = "123";
        order1.Status = WorkRequestStatus.Assigned;
        var order2 = new WorkRequest();
        order2.Creator = employee1;
        order2.Assignee = employee2;
        order2.Number = "456";
        order2.Status = WorkRequestStatus.Draft;

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee1);
            context.Add(employee2);
            context.Add(order1);
            context.Add(order2);
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkRequestQueryHandler(dataContext);
        var specification = new WorkRequestSearchSpecification();
        specification.MatchStatus(WorkRequestStatus.Assigned);
        var orders = await repository.GetWorkRequestsAsync(specification);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchWithEmptySpecificationAndReturnAll()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("1", "1", "1", "1");
        var order1 = new WorkRequest { Creator = employee, Assignee = employee, Number = "123" };
        var order2 = new WorkRequest { Creator = employee, Assignee = employee, Number = "456" };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(order1);
            context.Add(order2);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkRequestQueryHandler(dataContext);
        var orders = await repository.GetWorkRequestsAsync(new WorkRequestSearchSpecification());

        orders.Length.ShouldBe(2);
        orders.Any(o => o.Id == order1.Id).ShouldBeTrue();
        orders.Any(o => o.Id == order2.Id).ShouldBeTrue();
    }


    [Test]
    public void SearchShouldReturnHydratedEmployeesWithWorkRequests()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("1", "John", "Doe", "john.doe@example.com");
        var assignee = new Employee("2", "Jane", "Smith", "jane.smith@example.com");

        var order1 = new WorkRequest
        {
            Creator = creator,
            Assignee = assignee,
            Number = "123",
            Title = "Fix plumbing",
            Description = "Fix the plumbing in room 101",
            RoomNumber = "101",
            Status = WorkRequestStatus.InProgress
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(order1);
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkRequestQueryHandler(dataContext);

        var specification = new WorkRequestSearchSpecification();
        specification.MatchCreator(creator);

        var orders = repository.GetWorkRequestsAsync(specification).Result;

        orders.Length.ShouldBe(1);

        var rehydratedOrder = orders.First(o => o.Number == "123");

        rehydratedOrder.Creator.ShouldNotBeNull();
        rehydratedOrder.Assignee.ShouldNotBeNull();
        rehydratedOrder.Creator.Id.ShouldBe(creator.Id);
        rehydratedOrder.Creator.FirstName.ShouldBe(creator.FirstName);
        rehydratedOrder.Creator.LastName.ShouldBe(creator.LastName);
        rehydratedOrder.Creator.EmailAddress.ShouldBe(creator.EmailAddress);
        rehydratedOrder.Assignee.Id.ShouldBe(assignee.Id);
        rehydratedOrder.Assignee.FirstName.ShouldBe(assignee.FirstName);
        rehydratedOrder.Assignee.LastName.ShouldBe(assignee.LastName);
        rehydratedOrder.Assignee.EmailAddress.ShouldBe(assignee.EmailAddress);
    }
}