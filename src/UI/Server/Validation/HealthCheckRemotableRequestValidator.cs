using ClearMeasure.Bootcamp.Core;
using FluentValidation;

namespace ClearMeasure.Bootcamp.UI.Server.Validation;

public sealed class HealthCheckRemotableRequestValidator : AbstractValidator<HealthCheckRemotableRequest>
{
    public HealthCheckRemotableRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
