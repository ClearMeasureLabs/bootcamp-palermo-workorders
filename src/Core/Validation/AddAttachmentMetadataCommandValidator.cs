using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="AddAttachmentMetadataCommand"/> so bad attachment metadata is rejected
/// before it is stored.
/// </summary>
public sealed class AddAttachmentMetadataCommandValidator : AbstractValidator<AddAttachmentMetadataCommand>
{
    public AddAttachmentMetadataCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty();
        RuleFor(x => x.FileSize).GreaterThan(0);
    }
}
