using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="RoleByNameQuery"/>.
/// </summary>
public sealed class RoleByNameQueryValidator : AbstractValidator<RoleByNameQuery>
{
    public RoleByNameQueryValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}
