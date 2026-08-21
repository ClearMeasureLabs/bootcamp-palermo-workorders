using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class HealthCheckRemotableRequestValidatorTests
{
    private HealthCheckRemotableRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new HealthCheckRemotableRequestValidator();
    }

    [TestCase(HealthStatus.Healthy)]
    [TestCase(HealthStatus.Degraded)]
    [TestCase(HealthStatus.Unhealthy)]
    public void ShouldPass_WhenStatusIsDefinedEnumValue(HealthStatus status)
    {
        var request = new HealthCheckRemotableRequest(status);

        var result = _validator.TestValidate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenStatusIsUndefinedEnumValue()
    {
        var request = new HealthCheckRemotableRequest((HealthStatus)999);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }
}
