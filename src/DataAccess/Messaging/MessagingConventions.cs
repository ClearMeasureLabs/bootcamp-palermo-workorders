namespace ClearMeasure.Bootcamp.DataAccess.Messaging;

public class MessagingConventions : IMessageConvention
{
    public string Name => nameof(MessagingConventions);

    public bool IsEventType(Type type) => type.Name.EndsWith("event", StringComparison.InvariantCultureIgnoreCase);

    public bool IsCommandType(Type type) => type.Name.EndsWith("command", StringComparison.InvariantCultureIgnoreCase);

    public bool IsMessageType(Type type) => type.Name.EndsWith("message", StringComparison.InvariantCultureIgnoreCase);
}