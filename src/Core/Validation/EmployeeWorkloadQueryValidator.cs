using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Rejects blank usernames for <see cref="EmployeeWorkloadQuery"/>.
/// </summary>
public sealed class EmployeeWorkloadQueryValidator : AbstractValidator<EmployeeWorkloadQuery>
{
    public EmployeeWorkloadQueryValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
    }
}
