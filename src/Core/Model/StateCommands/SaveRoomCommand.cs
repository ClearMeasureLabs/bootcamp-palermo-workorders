// ReSharper disable PropertyCanBeMadeInitOnly.Global -- System.Text.Json set-by-convention requires mutable setters

using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public class SaveRoomCommand : IRequest<Room>, IRemotableRequest
{
    // ReSharper disable once UnusedMember.Global -- required for System.Text.Json deserialization (set-by-convention)
    public SaveRoomCommand()
    {
        Room = null!;
    }

    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    public SaveRoomCommand(Room room)
    {
        Room = room;
    }

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global -- System.Text.Json set-by-convention requires mutable setter
    public Room Room { get; set; }
}
