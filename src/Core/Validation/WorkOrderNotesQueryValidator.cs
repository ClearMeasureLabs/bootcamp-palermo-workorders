using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class WorkOrderNotesQueryValidator : AbstractValidator<WorkOrderNotesQuery>
{
    public WorkOrderNotesQueryValidator()
    {
        RuleFor(x => x.WorkOrderId).NotEmpty();
    }
}
