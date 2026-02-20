using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelTests
{
    [Test]
    public void Title_WhenEmpty_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = "", Description = "Test description" };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.MemberNames.Contains("Title"));
        validationResults.ShouldContain(r => r.ErrorMessage == "Title is required");
    }

    [Test]
    public void Title_WhenNull_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = null, Description = "Test description" };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.MemberNames.Contains("Title"));
        validationResults.ShouldContain(r => r.ErrorMessage == "Title is required");
    }

    [Test]
    public void Description_WhenEmpty_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = "Test title", Description = "" };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.MemberNames.Contains("Description"));
        validationResults.ShouldContain(r => r.ErrorMessage == "Description is required");
    }

    [Test]
    public void Description_WhenNull_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = "Test title", Description = null };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.MemberNames.Contains("Description"));
        validationResults.ShouldContain(r => r.ErrorMessage == "Description is required");
    }

    [Test]
    public void TitleAndDescription_WhenBothValid_ShouldPassValidation()
    {
        var model = new WorkOrderManageModel { Title = "Test title", Description = "Test description" };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeTrue();
        validationResults.ShouldBeEmpty();
    }

    [Test]
    public void TitleAndDescription_WhenBothEmpty_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = "", Description = "" };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.MemberNames.Contains("Title"));
        validationResults.ShouldContain(r => r.MemberNames.Contains("Description"));
        validationResults.ShouldContain(r => r.ErrorMessage == "Title is required");
        validationResults.ShouldContain(r => r.ErrorMessage == "Description is required");
    }
}
