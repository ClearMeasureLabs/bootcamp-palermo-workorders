using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class WorkRequestAttachmentsQueryValidator : AbstractValidator<WorkRequestAttachmentsQuery>
{
    public WorkRequestAttachmentsQueryValidator()
    {
        RuleFor(x => x.WorkRequestId).NotEmpty();
    }
}
