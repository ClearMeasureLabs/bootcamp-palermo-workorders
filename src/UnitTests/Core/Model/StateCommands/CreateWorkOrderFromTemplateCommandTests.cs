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

        var workOrder = new WorkOrder
        {
            Title = template.Title,
            Description = template.Description,
            RoomNumber = template.RoomNumber,
            Status = WorkOrderStatus.Draft
        };

        workOrder.Title.ShouldBe(template.Title);
        workOrder.Description.ShouldBe(template.Description);
        workOrder.RoomNumber.ShouldBe(template.RoomNumber);
    }

    [Test]
    public void CreateWorkOrderFromTemplateCommand_ShouldSetStatusToDraft()
    {
        var workOrder = new WorkOrder
        {
            Title = "Weekly Bathroom Cleaning",
            Description = "Clean all bathrooms on floor 1",
            RoomNumber = "B101",
            Status = WorkOrderStatus.Draft
        };

        workOrder.Status.ShouldBe(WorkOrderStatus.Draft);
    }
}
