using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

/// <summary>
/// Provides AI tools for use with an <see cref="IChatClient"/>.
/// </summary>
public interface IToolProvider
{
    IList<AITool> GetTools();
}
