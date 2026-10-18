using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class EmployeeByEmailQueryValidator : AbstractValidator<EmployeeByEmailQuery>
{
    public EmployeeByEmailQueryValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
    }
}
