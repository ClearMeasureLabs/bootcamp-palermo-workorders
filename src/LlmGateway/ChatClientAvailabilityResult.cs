namespace ClearMeasure.Bootcamp.LlmGateway;

public class ChatClientAvailabilityResult(bool isAvailable, string message)
{
    public bool IsAvailable { get; } = isAvailable;
    public string Message { get; } = message;
}
