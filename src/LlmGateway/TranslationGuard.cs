using System.Text.RegularExpressions;

namespace ClearMeasure.Bootcamp.LlmGateway;

internal static partial class TranslationGuard
{
    [GeneratedRegex(@"^[a-zA-Z]{2,3}(-[a-zA-Z0-9]{1,8})*$")]
    private static partial Regex Bcp47Regex();

    public static bool ShouldReturnOriginal(string? text, string targetLanguageCode) =>
        string.IsNullOrEmpty(text)
        || targetLanguageCode == "en-US"
        || !Bcp47Regex().IsMatch(targetLanguageCode);
}
