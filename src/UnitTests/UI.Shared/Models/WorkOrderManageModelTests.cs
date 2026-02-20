using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelTests
{
    [Test]
    public void Title_WithNullValue_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = null, Description = "Valid description" };
        var validationContext = new ValidationContext(model, null, null);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.ErrorMessage == "Title is required");
    }

    [Test]
    public void Title_WithEmptyValue_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = "", Description = "Valid description" };
        var validationContext = new ValidationContext(model, null, null);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.ErrorMessage == "Title is required");
    }

    [Test]
    public void Description_WithNullValue_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = "Valid title", Description = null };
        var validationContext = new ValidationContext(model, null, null);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.ErrorMessage == "Description is required");
    }

    [Test]
    public void Description_WithEmptyValue_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = "Valid title", Description = "" };
        var validationContext = new ValidationContext(model, null, null);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.ErrorMessage == "Description is required");
    }

    [Test]
    public void TitleAndDescription_WithNullValues_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = null, Description = null };
        var validationContext = new ValidationContext(model, null, null);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.Count.ShouldBe(2);
        validationResults.ShouldContain(r => r.ErrorMessage == "Title is required");
        validationResults.ShouldContain(r => r.ErrorMessage == "Description is required");
    }

    [Test]
    public void TitleAndDescription_WithValidValues_ShouldPassValidation()
    {
        var model = new WorkOrderManageModel { Title = "Valid title", Description = "Valid description" };
        var validationContext = new ValidationContext(model, null, null);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeTrue();
        validationResults.Count.ShouldBe(0);
    }
}
