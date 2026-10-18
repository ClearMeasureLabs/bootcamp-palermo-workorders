using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class EmployeeByUserNameQueryValidator : AbstractValidator<EmployeeByUserNameQuery>
{
    public EmployeeByUserNameQueryValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Username)
            .Must(username => username.All(c => !char.IsWhiteSpace(c)))
            .WithMessage("Username must not contain spaces.");
    }
}
