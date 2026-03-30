using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.UI.Server.Validation;

public sealed class EmployeeByUserNameQueryValidator : AbstractValidator<EmployeeByUserNameQuery>
{
    public EmployeeByUserNameQueryValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
    }
}
