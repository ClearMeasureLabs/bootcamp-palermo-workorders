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

        var langPrefix = preferredLanguage.Split('-')[0];
        var matchingVoice = voices.FirstOrDefault(v =>
            v.Lang?.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase) == true);
        if (matchingVoice != null)
        {
            utterance.Voice = matchingVoice;
        }

        return utterance;
    }
}
