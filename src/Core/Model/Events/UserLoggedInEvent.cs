namespace ClearMeasure.Bootcamp.Core.Model.Events;

/// <summary>Reports a demo-session login without exposing the reported identity to bus telemetry.</summary>
public record UserLoggedInEvent([property: ExcludeFromBusActivity] string UserName) : IRemotableEvent;
