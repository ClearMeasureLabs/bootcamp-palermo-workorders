using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class CreateWorkOrderFromTemplateCommandTests
{
    [Test]
    public void CreateWorkOrderFromTemplateCommand_ShouldCopyFields()
    {
        var template = new WorkOrderTemplate
        {
            Id = Guid.NewGuid(),
            Title = "Weekly Bathroom Cleaning",
            Description = "Clean all bathrooms on floor 1",
            RoomNumber = "B101",
            IsActive = true,
            CreatedById = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow
        };

        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jeff@example.com");
        var createdDate = new DateTime(2026, 1, 1, 12, 0, 0);

        var workOrder = template.ToWorkOrder(creator, "WO-001", createdDate);

        workOrder.Title.ShouldBe(template.Title);
        workOrder.Description.ShouldBe(template.Description);
        workOrder.RoomNumber.ShouldBe(template.RoomNumber);
        workOrder.Creator.ShouldBe(creator);
        workOrder.Number.ShouldBe("WO-001");
        workOrder.CreatedDate.ShouldBe(createdDate);
    }

    [Test]
    public void CreateWorkOrderFromTemplateCommand_ShouldSetStatusToDraft()
    {
        var template = new WorkOrderTemplate
        {
            Id = Guid.NewGuid(),
            Title = "Weekly Bathroom Cleaning",
            Description = "Clean all bathrooms on floor 1",
            RoomNumber = "B101",
            IsActive = true,
            CreatedById = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow
        };

        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jeff@example.com");

        var workOrder = template.ToWorkOrder(creator, "WO-001", DateTime.UtcNow);

        workOrder.Status.ShouldBe(WorkOrderStatus.Draft);
    }

    [Test]
    public void CreateWorkOrderFromTemplateCommand_ShouldHoldTemplateIdAndCreatorId()
    {
        var templateId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();

        var command = new CreateWorkOrderFromTemplateCommand(templateId, creatorId);

        command.TemplateId.ShouldBe(templateId);
        command.CreatorId.ShouldBe(creatorId);
    }
}
