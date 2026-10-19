using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="UpdatePreferredLanguageCommand"/>.
/// </summary>
public sealed class UpdatePreferredLanguageCommandValidator : AbstractValidator<UpdatePreferredLanguageCommand>
{
    private static readonly string[] AllowedLanguages = ["en-US", "es-ES", "fr-FR"];

    public UpdatePreferredLanguageCommandValidator()
    {
        RuleFor(command => command.Username).NotEmpty();
        RuleFor(command => command.PreferredLanguage)
            .Must(language => AllowedLanguages.Contains(language))
            .WithMessage("Preferred language must be en-US, es-ES, or fr-FR.");
    }
}
