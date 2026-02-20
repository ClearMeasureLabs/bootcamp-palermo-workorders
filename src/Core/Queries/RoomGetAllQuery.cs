using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record RoomGetAllQuery : IRequest<Room[]>, IRemotableRequest;
