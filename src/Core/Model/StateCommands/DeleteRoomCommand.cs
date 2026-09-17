// ReSharper disable PropertyCanBeMadeInitOnly.Global -- System.Text.Json set-by-convention requires mutable setters

using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public class DeleteRoomCommand : IRequest<Unit>, IRemotableRequest
{
    // ReSharper disable once UnusedMember.Global -- required for System.Text.Json deserialization (set-by-convention)
    public DeleteRoomCommand()
    {
    }

    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    public DeleteRoomCommand(Guid roomId)
    {
        RoomId = roomId;
    }

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global -- System.Text.Json set-by-convention requires mutable setter
    public Guid RoomId { get; set; }
}
