using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;
using System.ComponentModel.DataAnnotations;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelTests
{
    [Test]
    public void Title_WithOnlyLetters_ShouldBeValid()
    {
        var model = new WorkOrderManageModel
        {
            Title = "ReplaceWindowLatch",
            Description = "Test description"
        };

        var validationResults = ValidateModel(model);
        
        validationResults.ShouldBeEmpty();
    }

    [Test]
    public void Title_WithNumbers_ShouldBeInvalid()
    {
        var model = new WorkOrderManageModel
        {
            Title = "Replace123Windows",
            Description = "Test description"
        };

        var validationResults = ValidateModel(model);
        
        validationResults.Count.ShouldBe(1);
        validationResults[0].ErrorMessage.ShouldNotBeNull();
        validationResults[0].ErrorMessage!.ShouldContain("Title must contain only letters");
    }

    [Test]
    public void Title_WithSpecialCharacters_ShouldBeInvalid()
    {
        var model = new WorkOrderManageModel
        {
            Title = "Replace-Window!",
            Description = "Test description"
        };

        var validationResults = ValidateModel(model);
        
        validationResults.Count.ShouldBe(1);
        validationResults[0].ErrorMessage.ShouldNotBeNull();
        validationResults[0].ErrorMessage!.ShouldContain("Title must contain only letters");
    }

    [Test]
    public void Title_WithSpaces_ShouldBeInvalid()
    {
        var model = new WorkOrderManageModel
        {
            Title = "Replace Window Latch",
            Description = "Test description"
        };

        var validationResults = ValidateModel(model);
        
        validationResults.Count.ShouldBe(1);
        validationResults[0].ErrorMessage.ShouldNotBeNull();
        validationResults[0].ErrorMessage!.ShouldContain("Title must contain only letters");
    }

    [Test]
    public void Title_WithMixedCase_ShouldBeValid()
    {
        var model = new WorkOrderManageModel
        {
            Title = "FixSinkInBathroom",
            Description = "Test description"
        };

        var validationResults = ValidateModel(model);
        
        validationResults.ShouldBeEmpty();
    }

    [Test]
    public void Title_Empty_ShouldBeInvalid()
    {
        var model = new WorkOrderManageModel
        {
            Title = "",
            Description = "Test description"
        };

        var validationResults = ValidateModel(model);
        
        validationResults.Count.ShouldBeGreaterThanOrEqualTo(1);
    }

    private List<ValidationResult> ValidateModel(WorkOrderManageModel model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);
        return validationResults;
    }
}
