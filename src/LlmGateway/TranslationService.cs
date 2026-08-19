using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

public partial class TranslationService(ChatClientFactory chatClientFactory) : ITranslationService
{
    public async Task<string> TranslateAsync(string text, string targetLanguageCode)
    {
        if (TranslationGuard.ShouldReturnOriginal(text, targetLanguageCode))
        {
            return text ?? string.Empty;
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
