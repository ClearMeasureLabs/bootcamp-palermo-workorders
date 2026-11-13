namespace ClearMeasure.Bootcamp.Core.Model.Events;

/// <summary>Reports a demo-session logout for durable server-side auditing.</summary>
public record UserLoggedOutEvent([property: ExcludeFromBusActivity] string? UserName) : IRemotableEvent;
