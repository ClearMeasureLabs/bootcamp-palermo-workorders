using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>
/// Command to record a user login event for telemetry.
/// </summary>
public record RecordUserLoginCommand(string UserName) : IRequest<Unit>, IRemotableRequest;
