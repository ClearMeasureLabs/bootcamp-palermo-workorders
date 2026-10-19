using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class UpdatePreferredLanguageCommandValidatorTests
{
    [Test]
    public void ShouldPass_WhenLanguageIsAllowed()
    {
        var validator = new UpdatePreferredLanguageCommandValidator();

        var result = validator.TestValidate(new UpdatePreferredLanguageCommand("hsimpson", "es-ES"));

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenLanguageIsNotAllowed()
    {
        var validator = new UpdatePreferredLanguageCommandValidator();

        var result = validator.TestValidate(new UpdatePreferredLanguageCommand("hsimpson", "de-DE"));

        result.IsValid.ShouldBeFalse();
    }

    [Test]
    public void ShouldFail_WhenUsernameIsEmpty()
    {
        var validator = new UpdatePreferredLanguageCommandValidator();

        var result = validator.TestValidate(new UpdatePreferredLanguageCommand("", "en-US"));

        result.IsValid.ShouldBeFalse();
    }
}
