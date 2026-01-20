using ClearMeasure.Bootcamp.UI.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelTests
{
    [Test]
    public void Title_WithExactly12Characters_PassesValidation()
    {
        var model = new WorkOrderManageModel
        {
            Title = "Valid12Chars",
            Description = "Test Description"
        };

        var validationContext = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, validationContext, results, true);

        isValid.ShouldBeTrue();
        results.ShouldBeEmpty();
    }

    [Test]
    public void Title_WithLessThan12Characters_FailsValidation()
    {
        var model = new WorkOrderManageModel
        {
            Title = "Short",
            Description = "Test Description"
        };

        var validationContext = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, validationContext, results, true);

        isValid.ShouldBeFalse();
        results.ShouldNotBeEmpty();
        results[0].ErrorMessage.ShouldContain("12 characters");
    }

    [Test]
    public void Title_WithMoreThan12Characters_FailsValidation()
    {
        var model = new WorkOrderManageModel
        {
            Title = "ThisIsTooLong",
            Description = "Test Description"
        };

        var validationContext = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, validationContext, results, true);

        isValid.ShouldBeFalse();
        results.ShouldNotBeEmpty();
        results[0].ErrorMessage.ShouldContain("12 characters");
    }

    [Test]
    public void Title_WithNull_FailsValidation()
    {
        var model = new WorkOrderManageModel
        {
            Title = null,
            Description = "Test Description"
        };

        var validationContext = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, validationContext, results, true);

        isValid.ShouldBeFalse();
        results.ShouldNotBeEmpty();
    }

    [Test]
    public void Title_WithEmptyString_FailsValidation()
    {
        var model = new WorkOrderManageModel
        {
            Title = "",
            Description = "Test Description"
        };

        var validationContext = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, validationContext, results, true);

        isValid.ShouldBeFalse();
        results.ShouldNotBeEmpty();
    }
}
