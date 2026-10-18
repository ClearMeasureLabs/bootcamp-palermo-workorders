using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="OpenWorkOrderCountByAssigneeQuery"/>.
/// </summary>
public sealed class OpenWorkOrderCountByAssigneeQueryValidator
    : AbstractValidator<OpenWorkOrderCountByAssigneeQuery>
{
    public OpenWorkOrderCountByAssigneeQueryValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
    }
}
