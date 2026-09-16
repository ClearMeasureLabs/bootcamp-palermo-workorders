using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.UI.Server.Validation;

// ReSharper disable once ClassNeverInstantiated.Global -- registered by DI (FluentValidation assembly scan)
public sealed class EmployeeGetAllQueryValidator : AbstractValidator<EmployeeGetAllQuery>;
