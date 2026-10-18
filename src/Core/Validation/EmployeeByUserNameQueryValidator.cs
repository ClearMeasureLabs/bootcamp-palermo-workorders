using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class EmployeeByUserNameQueryValidator : AbstractValidator<EmployeeByUserNameQuery>
{
    public EmployeeByUserNameQueryValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .Must(username => !username.Any(char.IsWhiteSpace))
            .WithMessage("Username must not contain whitespace.");
    }
}
