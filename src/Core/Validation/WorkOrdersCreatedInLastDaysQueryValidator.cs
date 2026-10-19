using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="WorkOrdersCreatedInLastDaysQuery"/>. Rejects a day window outside 1–30.
/// </summary>
public sealed class WorkOrdersCreatedInLastDaysQueryValidator : AbstractValidator<WorkOrdersCreatedInLastDaysQuery>
{
    public WorkOrdersCreatedInLastDaysQueryValidator()
    {
        RuleFor(x => x.Days).InclusiveBetween(1, 30);
    }
}
