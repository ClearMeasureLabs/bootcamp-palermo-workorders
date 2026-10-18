using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record RoleByNameQuery(string Name) : IRequest<Role?>, IRemotableRequest;
