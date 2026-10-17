using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class SaveDraftCommandValidator : AbstractValidator<SaveDraftCommand>
{
    public SaveDraftCommandValidator()
    {
        RuleFor(x => x.WorkOrder.DueDate)
            .Must(dueDate => dueDate is null || dueDate >= ChurchTimeZone.Today(TimeProvider.System))
            .WithMessage("Due date cannot be earlier than today.");
    }
}
