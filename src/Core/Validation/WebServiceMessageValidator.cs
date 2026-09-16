using ClearMeasure.Bootcamp.Core.Messaging;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

// ReSharper disable once ClassNeverInstantiated.Global -- registered by DI (FluentValidation assembly scan)
public sealed class WebServiceMessageValidator : AbstractValidator<WebServiceMessage>
{
    public WebServiceMessageValidator()
    {
        RuleFor(x => x.TypeName).NotEmpty();
        RuleFor(x => x.Body).NotEmpty();
    }
}
