using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="CreateDatedWorkOrdersCommand"/> so the remotable command is accepted by
/// the single-API validation middleware.
/// </summary>
public sealed class CreateDatedWorkOrdersCommandValidator : AbstractValidator<CreateDatedWorkOrdersCommand>
{
    public CreateDatedWorkOrdersCommandValidator()
    {
        RuleFor(x => x.CreatorUsername).NotEmpty();
        RuleFor(x => x.AssigneeUsername).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(WorkOrder.TitleMaxLength);
        RuleFor(x => x.DueDates)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(dates => dates.Count <= CreateDatedWorkOrdersCommand.MaximumBatchSize)
            .WithMessage($"At most {CreateDatedWorkOrdersCommand.MaximumBatchSize} due dates are allowed.");
    }
}
