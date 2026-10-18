using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="WorkOrderCountByAssigneeQuery"/>.
/// </summary>
public sealed class WorkOrderCountByAssigneeQueryValidator : AbstractValidator<WorkOrderCountByAssigneeQuery>
{
    public WorkOrderCountByAssigneeQueryValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
    }
}
