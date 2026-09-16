using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public class DeleteRoomCommand : IRequest<Unit>
{
    public DeleteRoomCommand()
    {
    }

    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    public DeleteRoomCommand(Guid roomId)
    {
        RoomId = roomId;
    }

    public Guid RoomId { get; set; }
}
