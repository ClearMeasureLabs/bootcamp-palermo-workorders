using ClearMeasure.Bootcamp.Core.Messaging;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class WebServiceMessageValidator : AbstractValidator<WebServiceMessage>
{
    public WebServiceMessageValidator()
    {
        RuleFor(x => x.TypeName).NotEmpty();
        RuleFor(x => x.Body).NotEmpty();
    }
}
