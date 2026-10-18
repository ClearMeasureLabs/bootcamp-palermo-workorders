using Toolbelt.Blazor.SpeechSynthesis;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

internal static class WorkOrderSpeechHelper
{
    public static SpeechSynthesisUtterance CreateUtterance(
        string translatedText,
        string preferredLanguage,
        IEnumerable<SpeechSynthesisVoice> voices)
    {
        var utterance = new SpeechSynthesisUtterance
        {
            Text = translatedText,
            Lang = preferredLanguage
        };

        var matchingVoice = FindMatchingVoice(voices, preferredLanguage);
        if (matchingVoice != null)
        {
            utterance.Voice = matchingVoice;
        }

        return utterance;
    }

    private static SpeechSynthesisVoice? FindMatchingVoice(
        IEnumerable<SpeechSynthesisVoice> voices,
        string preferredLanguage)
    {
        var exactMatch = voices.FirstOrDefault(v =>
            v.Lang.Equals(preferredLanguage, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
        {
            return exactMatch;
        }

        var langPrefix = preferredLanguage.Split('-')[0];
        return voices.FirstOrDefault(v =>
            v.Lang.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase));
    }
}
