using ClearMeasure.Bootcamp.Core;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Commands;

/// <summary>
/// Command to record a user login event. Sent from WASM client to server for OTEL metric recording.
/// </summary>
public class RecordUserLoginCommand : IRequest<Unit>, IRemotableRequest
{
    public required string UserName { get; set; }
    public required string FullName { get; set; }
}