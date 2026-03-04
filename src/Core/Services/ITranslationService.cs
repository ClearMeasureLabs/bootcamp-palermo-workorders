namespace ClearMeasure.Bootcamp.Core.Services;

public interface ITranslationService
{
    Task<string> TranslateAsync(string text, string targetLanguageCode);
}
