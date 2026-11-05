using System.Globalization;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

/// <summary>
/// Local Ollama defaults for Jeffrey's RTX 5070 Ti: Qwen GGUF alias with a hard GPU-safe context ceiling.
/// </summary>
public static class OllamaChatDefaults
{
    /// <summary>Ollama model alias for ISTA-DASLab Qwen3.8-27B GSQ-RCO IQ3_XXS.</summary>
    public const string DefaultModelId = "qwen38-27b-gsq-rco";

    /// <summary>
    /// Maximum <c>num_ctx</c>. Measured GPU-bound through 49152; 65536 spills and hangs.
    /// </summary>
    public const int MaxNumCtx = 49152;

    /// <summary>Ollama <c>num_ctx</c> option name.</summary>
    public const string NumCtxPropertyName = "num_ctx";

    /// <summary>Default local Ollama endpoint.</summary>
    public static readonly Uri LocalEndpoint = new("http://localhost:11434/");

    /// <summary>
    /// Copies <paramref name="options"/> and sets <c>num_ctx</c> to at most <see cref="MaxNumCtx"/>.
    /// Callers cannot raise context above the GPU-safe ceiling.
    /// </summary>
    public static ChatOptions WithCappedNumCtx(ChatOptions? options)
    {
        var result = options?.Clone() ?? new ChatOptions();
        result.AdditionalProperties ??= [];

        var requested = MaxNumCtx;
        if (result.AdditionalProperties.TryGetValue(NumCtxPropertyName, out var raw) && raw is not null)
        {
            requested = Convert.ToInt32(raw, CultureInfo.InvariantCulture);
        }

        if (requested > MaxNumCtx || requested < 1)
        {
            requested = MaxNumCtx;
        }

        result.AdditionalProperties[NumCtxPropertyName] = requested;
        return result;
    }
}
