using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="DraftToCancelledCommand"/>. Domain rules live on the command;
/// this type exists so remoting validation middleware accepts the request.
/// </summary>
public sealed class DraftToCancelledCommandValidator : AbstractValidator<DraftToCancelledCommand>;
