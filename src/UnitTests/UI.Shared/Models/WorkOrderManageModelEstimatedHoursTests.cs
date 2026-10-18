using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelEstimatedHoursTests
{
    [Test]
    public void EstimatedHours_WhenWithinRange_ShouldBeValid()
    {
        var model = ValidModel(12.5m);
        Validate(model).ShouldBeEmpty();
    }

    [Test]
    public void EstimatedHours_WhenNull_ShouldBeValid()
    {
        var model = ValidModel(null);
        Validate(model).ShouldBeEmpty();
    }

    [Test]
    public void EstimatedHours_WhenAboveMax_ShouldBeInvalid()
    {
        var model = ValidModel(1000m);
        Validate(model).ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.EstimatedHours)));
    }

    [Test]
    public void EstimatedHours_WhenNegative_ShouldBeInvalid()
    {
        var model = ValidModel(-0.01m);
        Validate(model).ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.EstimatedHours)));
    }

    private static WorkOrderManageModel ValidModel(decimal? estimatedHours) =>
        new()
        {
            Title = "Title",
            Description = "Description",
            EstimatedHours = estimatedHours
        };

    private static List<ValidationResult> Validate(WorkOrderManageModel model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }
}
