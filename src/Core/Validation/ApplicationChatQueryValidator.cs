using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Remoting envelope requires a registered FluentValidation validator for every payload type.
/// </summary>
public sealed class ApplicationChatQueryValidator : AbstractValidator<ApplicationChatQuery>;
