using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record UpdatePreferredLanguageCommand(string Username, string PreferredLanguage) :
    IRequest<Employee>, IRemotableRequest;
