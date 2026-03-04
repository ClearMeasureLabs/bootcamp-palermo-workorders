using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;
using System.ComponentModel.DataAnnotations;

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

        var results = new List<ValidationResult>();
        var context = new ValidationContext(template);
        var isValid = Validator.TryValidateObject(template, context, results, validateAllProperties: true);

        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderTemplate.Title)));
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
