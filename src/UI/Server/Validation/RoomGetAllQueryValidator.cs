using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.UI.Server.Validation;

/// <summary>
/// Validator for the client-originated <see cref="RoomGetAllQuery"/> remotable request.
/// The query carries no data, so there are no rules; it exists so the
/// WebServiceMessage validation middleware finds a registered validator and
/// lets the request through (an unregistered payload type is rejected with 400).
/// </summary>
public sealed class RoomGetAllQueryValidator : AbstractValidator<RoomGetAllQuery>;
