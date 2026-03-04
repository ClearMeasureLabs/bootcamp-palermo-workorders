using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class TranslationService(ChatClientFactory chatClientFactory) : ITranslationService
{
    public async Task<string> TranslateAsync(string text, string targetLanguageCode)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (targetLanguageCode == "en-US")
        {
            return text;
        }

        var availability = await chatClientFactory.IsChatClientAvailable();
        if (!availability.IsAvailable)
        {
            return text;
        }

        var chatClient = await chatClientFactory.GetChatClient();

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
