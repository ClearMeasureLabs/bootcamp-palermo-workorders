using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record WeeklyCompletedSummaryQuery : IRequest<int>, IRemotableRequest;
