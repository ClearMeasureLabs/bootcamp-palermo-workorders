using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderTemplateTests
{
    [Test]
    public void WorkOrderTemplate_ShouldRequireTitle()
    {
        var template = new WorkOrderTemplate
        {
            Id = Guid.NewGuid(),
            Title = "",
            Description = "Weekly cleaning procedure",
            RoomNumber = "101",
            IsActive = true,
            CreatedById = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow
        };

        template.Title.ShouldBe("");
        template.Title.Length.ShouldBe(0);
    }

    [Test]
    public void WorkOrderTemplate_ShouldDefaultIsActiveToTrue()
    {
        var template = new WorkOrderTemplate
        {
            Id = Guid.NewGuid(),
            Title = "Weekly Cleaning",
            CreatedById = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow
        };

        template.IsActive.ShouldBeTrue();
    }
}
