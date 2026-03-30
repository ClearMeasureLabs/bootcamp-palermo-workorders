using ClearMeasure.Bootcamp.Core.Model.Events;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class UserLoggedInEventValidator : AbstractValidator<UserLoggedInEvent>
{
    public UserLoggedInEventValidator()
    {
        RuleFor(x => x.UserName).NotEmpty();
    }
}
