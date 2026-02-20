using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;
using System.ComponentModel.DataAnnotations;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelTests
{
    [Test]
    public void Title_WithEmptyValue_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel
        {
            Title = "",
            Description = "Valid description"
        };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(vr => vr.MemberNames.Contains("Title"));
        validationResults.First(vr => vr.MemberNames.Contains("Title")).ErrorMessage.ShouldBe("Title is required");
    }

    [Test]
    public void Title_WithNullValue_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel
        {
            Title = null,
            Description = "Valid description"
        };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(vr => vr.MemberNames.Contains("Title"));
        validationResults.First(vr => vr.MemberNames.Contains("Title")).ErrorMessage.ShouldBe("Title is required");
    }

    [Test]
    public void Description_WithEmptyValue_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel
        {
            Title = "Valid title",
            Description = ""
        };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(vr => vr.MemberNames.Contains("Description"));
        validationResults.First(vr => vr.MemberNames.Contains("Description")).ErrorMessage.ShouldBe("Description is required");
    }

    [Test]
    public void Description_WithNullValue_ShouldFailValidation()
    {
        var model = new WorkOrderManageModel
        {
            Title = "Valid title",
            Description = null
        };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(vr => vr.MemberNames.Contains("Description"));
        validationResults.First(vr => vr.MemberNames.Contains("Description")).ErrorMessage.ShouldBe("Description is required");
    }

    [Test]
    public void Model_WithValidTitleAndDescription_ShouldPassValidation()
    {
        var model = new WorkOrderManageModel
        {
            Title = "Valid title",
            Description = "Valid description"
        };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeTrue();
        validationResults.ShouldBeEmpty();
    }

    [Test]
    public void Model_WithBothEmptyValues_ShouldFailValidationForBothFields()
    {
        var model = new WorkOrderManageModel
        {
            Title = "",
            Description = ""
        };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.Count.ShouldBe(2);
        validationResults.ShouldContain(vr => vr.MemberNames.Contains("Title"));
        validationResults.ShouldContain(vr => vr.MemberNames.Contains("Description"));
    }
}
