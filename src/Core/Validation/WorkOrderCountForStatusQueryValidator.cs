using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="WorkOrderCountForStatusQuery"/>.
/// </summary>
public sealed class WorkOrderCountForStatusQueryValidator : AbstractValidator<WorkOrderCountForStatusQuery>
{
    public WorkOrderCountForStatusQueryValidator()
    {
        RuleFor(q => q.StatusKey)
            .NotEmpty()
            .Must(key => WorkOrderStatus.GetAllItems().Any(s => s.Key == key))
            .WithMessage("StatusKey must be a known work order status key.");
    }
}
