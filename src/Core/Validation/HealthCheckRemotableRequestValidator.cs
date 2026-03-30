using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class HealthCheckRemotableRequestValidator : AbstractValidator<HealthCheckRemotableRequest>
{
    public HealthCheckRemotableRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
