// ReSharper disable PropertyCanBeMadeInitOnly.Global -- Qodana P5 (#9440): NHibernate proxy / System.Text.Json set-by-convention requires mutable setters

using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Remotable one-paragraph AI summary of a work order's title, description, and instructions.
/// </summary>
public record WorkOrderSummarizeQuery(
    [property: ExcludeFromBusActivity] string Title,
    [property: ExcludeFromBusActivity] string Description,
    [property: ExcludeFromBusActivity] string Instructions)
    : IRequest<WorkOrderSummarizeResult>, IRemotableRequest;

/// <summary>
/// Serializable summarize reply for the WASM remoting boundary.
/// </summary>
public record WorkOrderSummarizeResult(string Summary);
