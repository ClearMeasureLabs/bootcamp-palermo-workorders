using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

public partial class TranslationService(ChatClientFactory chatClientFactory) : ITranslationService
{
    [GeneratedRegex(@"^[a-zA-Z]{2,3}(-[a-zA-Z0-9]{1,8})*$")]
    private static partial Regex Bcp47Regex();

    public async Task<string> TranslateAsync(string text, string targetLanguageCode)
    {
        if (string.IsNullOrEmpty(text))
        {
            // Qodana NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract false
            // positive: MediatR/remote request deserialization can pass a null text value
            // regardless of the non-nullable parameter annotation (see
            // ShouldReturnOriginalTextWhenInputIsNull). Null-coalescing retained intentionally.
#pragma warning disable CS8073, CS8825
            return text ?? string.Empty;
#pragma warning restore CS8073, CS8825
        }

        if (targetLanguageCode == "en-US")
        {
            return text;
        }

        if (!Bcp47Regex().IsMatch(targetLanguageCode))
        {
            return text;
        }

        IChatClient chatClient;
        try
        {
            chatClient = await chatClientFactory.GetChatClient();
        }
        catch
        {
            return text;
        }

        var systemPrompt =
            $"Translate the following text into the language identified by BCP 47 code '{targetLanguageCode}'. Return ONLY the translated text, nothing else.";

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, text)
        };

        var response = await chatClient.GetResponseAsync(messages);
        var translatedText = response.Text?.Trim();

        return string.IsNullOrWhiteSpace(translatedText) ? text : translatedText;
    }
}
