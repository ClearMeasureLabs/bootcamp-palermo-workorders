using ClearMeasure.Bootcamp.UI.Client.HealthChecks;
using FluentValidation;

namespace ClearMeasure.Bootcamp.UI.Server.Validation;

/// <summary>
/// Validator for the client-originated <see cref="ServerHealthCheckQuery"/> remotable request.
/// The query carries no data, so there are no rules; it exists so the
/// WebServiceMessage validation middleware finds a registered validator and
/// lets the request through (an unregistered payload type is rejected with 400).
/// </summary>
public sealed class ServerHealthCheckQueryValidator : AbstractValidator<ServerHealthCheckQuery>;
