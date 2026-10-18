using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class WorkOrderCountByCreatorQueryValidator : AbstractValidator<WorkOrderCountByCreatorQuery>
{
    public WorkOrderCountByCreatorQueryValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
    }
}
