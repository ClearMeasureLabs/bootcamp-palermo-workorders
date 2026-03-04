using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class WorkOrderTemplateTests : IntegratedTestBase
{
    [Test]
    public async Task WorkOrderTemplate_ShouldPersistAndRetrieve()
    {
        new DatabaseTests().Clean();

        var createdById = Guid.NewGuid();
        var template = new WorkOrderTemplate
        {
            Id = Guid.NewGuid(),
            Title = "Weekly Bathroom Cleaning",
            Description = "Clean all bathrooms on floor 1",
            RoomNumber = "B101",
            IsActive = true,
            CreatedById = createdById,
            CreatedDate = DateTime.UtcNow
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(template);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkOrderTemplateQueryHandler(dataContext);
        var retrieved = await handler.Handle(new WorkOrderTemplateByIdQuery(template.Id), default);

        retrieved.ShouldNotBeNull();
        retrieved.Title.ShouldBe(template.Title);
        retrieved.Description.ShouldBe(template.Description);
        retrieved.RoomNumber.ShouldBe(template.RoomNumber);
        retrieved.IsActive.ShouldBeTrue();
        retrieved.CreatedById.ShouldBe(createdById);
    }

    [Test]
    public async Task WorkOrderTemplatesQuery_ShouldReturnOnlyActiveTemplates()
    {
        new DatabaseTests().Clean();

        var activeTemplate = new WorkOrderTemplate
        {
            Id = Guid.NewGuid(),
            Title = "Active Template",
            IsActive = true,
            CreatedById = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow
        };
        var inactiveTemplate = new WorkOrderTemplate
        {
            Id = Guid.NewGuid(),
            Title = "Inactive Template",
            IsActive = false,
            CreatedById = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(activeTemplate);
            context.Add(inactiveTemplate);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkOrderTemplateQueryHandler(dataContext);
        var results = await handler.Handle(new WorkOrderTemplatesQuery(), default);

        results.Length.ShouldBe(1);
        results[0].Title.ShouldBe("Active Template");
    }

    [Test]
    public async Task CreateWorkOrderFromTemplateCommand_ShouldPersistNewWorkOrder()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("templateuser", "Template", "User", "template@example.com");
        creator.AddRole(new Role("admin", true, true));
        var template = new WorkOrderTemplate
        {
            Id = Guid.NewGuid(),
            Title = "Weekly Bathroom Cleaning",
            Description = "Clean all bathrooms",
            RoomNumber = "B101",
            IsActive = true,
            CreatedById = creator.Id,
            CreatedDate = DateTime.UtcNow
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(template);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var command = new CreateWorkOrderFromTemplateCommand(template.Id, creator.Id);
        var workOrder = await bus.Send(command);

        workOrder.ShouldNotBeNull();
        workOrder.Title.ShouldBe(template.Title);
        workOrder.Description.ShouldBe(template.Description);
        workOrder.RoomNumber.ShouldBe(template.RoomNumber);
        workOrder.Status.ShouldBe(WorkOrderStatus.Draft);
        workOrder.Creator.ShouldNotBeNull();
        workOrder.Creator.Id.ShouldBe(creator.Id);
        workOrder.Number.ShouldNotBeNullOrEmpty();
    }
}
