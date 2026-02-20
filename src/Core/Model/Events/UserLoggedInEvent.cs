namespace ClearMeasure.Bootcamp.Core.Model.Events;

public record UserLoggedInEvent(string UserName) : IRemotableEvent;
