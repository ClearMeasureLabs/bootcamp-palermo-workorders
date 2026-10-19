using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="WorkOrdersDueSoonQuery"/>. Days must be between 1 and 30 inclusive.
/// </summary>
public sealed class WorkOrdersDueSoonQueryValidator : AbstractValidator<WorkOrdersDueSoonQuery>
{
    public WorkOrdersDueSoonQueryValidator()
    {
        RuleFor(query => query.Days).InclusiveBetween(1, 30);
    }
}
