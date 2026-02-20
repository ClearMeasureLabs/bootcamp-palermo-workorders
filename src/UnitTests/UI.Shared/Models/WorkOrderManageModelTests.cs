using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelTests
{
    [Test]
    public void Title_WithEmptyValue_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = "", Description = "Valid Description" };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.MemberNames.Contains("Title"));
        validationResults.ShouldContain(r => r.ErrorMessage == "Title is required");
    }

    [Test]
    public void Title_WithNullValue_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = null, Description = "Valid Description" };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.MemberNames.Contains("Title"));
        validationResults.ShouldContain(r => r.ErrorMessage == "Title is required");
    }

    [Test]
    public void Description_WithEmptyValue_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = "Valid Title", Description = "" };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.MemberNames.Contains("Description"));
        validationResults.ShouldContain(r => r.ErrorMessage == "Description is required");
    }

    [Test]
    public void Description_WithNullValue_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = "Valid Title", Description = null };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.MemberNames.Contains("Description"));
        validationResults.ShouldContain(r => r.ErrorMessage == "Description is required");
    }

    [Test]
    public void WorkOrderManageModel_WithBothFieldsEmpty_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel { Title = "", Description = "" };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.Count.ShouldBe(2);
        validationResults.ShouldContain(r => r.MemberNames.Contains("Title"));
        validationResults.ShouldContain(r => r.MemberNames.Contains("Description"));
    }

    [Test]
    public void WorkOrderManageModel_WithValidFields_ShouldPassValidation()
    {
        var model = new WorkOrderManageModel { Title = "Valid Title", Description = "Valid Description" };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeTrue();
        validationResults.ShouldBeEmpty();
    }
}
