using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public class SaveRoomCommand : IRequest<Room>
{
    public SaveRoomCommand()
    {
        Room = null!;
    }

    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    public SaveRoomCommand(Room room)
    {
        Room = room;
    }

    public Room Room { get; set; }
}
