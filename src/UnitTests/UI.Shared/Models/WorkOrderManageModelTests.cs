using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelTests
{
    [Test]
    public void ShouldFailValidation_When_InstructionsExceed4000Characters()
    {
        var model = new WorkOrderManageModel
        {
            Title = "t",
            Description = "d",
            Instructions = new string('z', 4001)
        };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            results,
            validateAllProperties: true);

        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Instructions)));
    }

    [Test]
    public void ShouldPassValidation_When_InstructionsWithin4000Characters()
    {
        var model = new WorkOrderManageModel
        {
            Title = "t",
            Description = "d",
            Instructions = new string('z', 4000)
        };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            results,
            validateAllProperties: true);

        isValid.ShouldBeTrue();
    }
}
