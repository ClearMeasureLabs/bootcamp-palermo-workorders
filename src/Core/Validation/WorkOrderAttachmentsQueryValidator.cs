using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class WorkOrderAttachmentsQueryValidator : AbstractValidator<WorkOrderAttachmentsQuery>
{
    public WorkOrderAttachmentsQueryValidator()
    {
        RuleFor(x => x.WorkOrderId).NotEmpty();
    }
}
