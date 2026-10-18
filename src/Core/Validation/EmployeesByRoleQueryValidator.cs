using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class EmployeesByRoleQueryValidator : AbstractValidator<EmployeesByRoleQuery>
{
    public EmployeesByRoleQueryValidator()
    {
        RuleFor(x => x.RoleName).NotEmpty();
    }
}
