using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable once UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class WorkOrderSpecificationQueryValidator : AbstractValidator<WorkOrderSpecificationQuery>;
