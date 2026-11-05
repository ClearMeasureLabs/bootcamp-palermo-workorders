using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="OpenWorkOrderCountQuery"/>. The query carries no parameters, so no
/// rules apply; the validator exists so the single-API validation middleware accepts the request
/// instead of rejecting it with 400 "No validator registered".
/// </summary>
public sealed class OpenWorkOrderCountQueryValidator : AbstractValidator<OpenWorkOrderCountQuery>;
