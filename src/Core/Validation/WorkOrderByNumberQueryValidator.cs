using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class WorkOrderByNumberQueryValidator : AbstractValidator<WorkOrderByNumberQuery>
{
    public WorkOrderByNumberQueryValidator()
    {
        RuleFor(x => x.Number).NotEmpty();
        RuleFor(x => x.Number)
            .Must(number => !number.Any(char.IsWhiteSpace))
            .WithMessage("Work order number must not contain spaces.");
    }
}
