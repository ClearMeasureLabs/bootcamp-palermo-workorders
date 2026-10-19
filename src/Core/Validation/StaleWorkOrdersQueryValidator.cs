using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="StaleWorkOrdersQuery"/>. Days must be between 1 and 90 inclusive.
/// </summary>
public sealed class StaleWorkOrdersQueryValidator : AbstractValidator<StaleWorkOrdersQuery>
{
    public StaleWorkOrdersQueryValidator()
    {
        RuleFor(x => x.Days).InclusiveBetween(1, 90);
    }
}
