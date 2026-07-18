using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class WorkRequestByNumberQueryValidator : AbstractValidator<WorkRequestByNumberQuery>
{
    public WorkRequestByNumberQueryValidator()
    {
        RuleFor(x => x.Number).NotEmpty();
    }
}
