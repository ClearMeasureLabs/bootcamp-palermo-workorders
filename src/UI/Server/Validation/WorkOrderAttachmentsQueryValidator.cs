using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.UI.Server.Validation;

public sealed class WorkOrderAttachmentsQueryValidator : AbstractValidator<WorkOrderAttachmentsQuery>
{
    public WorkOrderAttachmentsQueryValidator()
    {
        RuleFor(x => x.WorkOrderId).NotEmpty();
    }
}
