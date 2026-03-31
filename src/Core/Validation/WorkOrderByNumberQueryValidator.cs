using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class WorkOrderByNumberQueryValidator : AbstractValidator<WorkOrderByNumberQuery>
{
    public WorkOrderByNumberQueryValidator()
    {
        RuleFor(x => x.Number).NotEmpty();
    }
}
