using ClearMeasure.Bootcamp.UI.Client.HealthChecks;
using ClearMeasure.Bootcamp.UI.Server.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Validation;

[TestFixture]
public class ServerHealthCheckQueryValidatorTests
{
    [Test]
    public void ShouldPass_WhenQueryHasNoFields()
    {
        var validator = new ServerHealthCheckQueryValidator();

        var result = validator.TestValidate(new ServerHealthCheckQuery());

        result.IsValid.ShouldBeTrue();
    }
}
