using ClearMeasure.Bootcamp.Core;
using MediatR;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.UI.Client.HealthChecks;

public record ServerHealthCheckQuery : IRequest<HealthStatus>, IRemotableRequest;