using MediatR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ClearMeasure.Bootcamp.Core;

namespace ClearMeasure.Bootcamp.UI.Client.HealthChecks;

public record ServerHealthCheckQuery : IRequest<HealthStatus>, IRemotableRequest;