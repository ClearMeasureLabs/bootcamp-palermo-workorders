using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.UI.Shared.Models;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelTests
{
    [Test]
    public void Title_With300Characters_ShouldPassValidation()
    {
        var title300 = new string('X', 300);
        var model = new WorkOrderManageModel
        {
            Title = title300,
            Description = "Test description"
        };

        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, context, results, true);

        isValid.ShouldBeTrue();
        results.ShouldBeEmpty();
    }

    [Test]
    public void Title_With301Characters_ShouldFailValidation()
    {
        var title301 = new string('X', 301);
        var model = new WorkOrderManageModel
        {
            Title = title301,
            Description = "Test description"
        };

        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, context, results, true);

        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains("Title"));
    }
}
