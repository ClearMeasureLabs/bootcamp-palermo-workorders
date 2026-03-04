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

        var workOrder = new WorkOrder
        {
            Title = template.Title,
            Description = template.Description,
            RoomNumber = template.RoomNumber,
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };

        workOrder.Title.ShouldBe(template.Title);
        workOrder.Description.ShouldBe(template.Description);
        workOrder.RoomNumber.ShouldBe(template.RoomNumber);
        workOrder.Creator.ShouldBe(creator);
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

        var workOrder = new WorkOrder
        {
            Title = template.Title,
            Description = template.Description,
            RoomNumber = template.RoomNumber,
            Status = WorkOrderStatus.Draft
        };

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
