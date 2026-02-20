using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelTests
{
    [Test]
    public void Title_WhenNull_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = null, Description = "Valid Description" };
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, context, results, true);

        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains("Title") && r.ErrorMessage == "Title is required");
    }

    [Test]
    public void Title_WhenEmpty_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = "", Description = "Valid Description" };
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, context, results, true);

        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains("Title") && r.ErrorMessage == "Title is required");
    }

    [Test]
    public void Description_WhenNull_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = "Valid Title", Description = null };
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, context, results, true);

        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains("Description") && r.ErrorMessage == "Description is required");
    }

    [Test]
    public void Description_WhenEmpty_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = "Valid Title", Description = "" };
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, context, results, true);

        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains("Description") && r.ErrorMessage == "Description is required");
    }

    [Test]
    public void TitleAndDescription_WhenBothEmpty_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = "", Description = "" };
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, context, results, true);

        isValid.ShouldBeFalse();
        results.Count.ShouldBeGreaterThanOrEqualTo(2);
        results.ShouldContain(r => r.MemberNames.Contains("Title") && r.ErrorMessage == "Title is required");
        results.ShouldContain(r => r.MemberNames.Contains("Description") && r.ErrorMessage == "Description is required");
    }

    [Test]
    public void TitleAndDescription_WhenBothValid_ShouldPassValidation()
    {
        var model = new WorkOrderManageModel { Title = "Valid Title", Description = "Valid Description" };
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, context, results, true);

        isValid.ShouldBeTrue();
        results.ShouldBeEmpty();
    }
}
