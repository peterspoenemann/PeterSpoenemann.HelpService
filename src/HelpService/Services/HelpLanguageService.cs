using PeterSpoenemann.HelpService.Resources;

namespace PeterSpoenemann.HelpService.Services;

internal sealed class HelpLanguageService : IHelpLanguageService
{
    private readonly HashSet<string> supportedLanguages;
    private string currentLanguage;

    public HelpLanguageService(IEnumerable<string> supportedLanguages, string initialLanguage)
    {
        this.supportedLanguages = new HashSet<string>(supportedLanguages, StringComparer.OrdinalIgnoreCase);
        currentLanguage = initialLanguage;
        SupportedLanguages = this.supportedLanguages
            .OrderBy(language => language == HelpLanguageCodes.German ? 0 : 1)
            .ThenBy(language => language, StringComparer.Ordinal)
            .ToArray();
    }

    public event EventHandler<HelpLanguageChangedEventArgs>? LanguageChanged;

    public string CurrentLanguage => currentLanguage;

    public IReadOnlyCollection<string> SupportedLanguages { get; }

    public void SetLanguage(string language)
    {
        if (!HelpLanguageCodes.TryNormalize(language, out var normalized)
            || !supportedLanguages.Contains(normalized))
        {
            throw new ArgumentException(
                HelpResources.Format("UnsupportedOrUnavailableLanguage", currentLanguage, language),
                nameof(language));
        }

        if (string.Equals(currentLanguage, normalized, StringComparison.Ordinal))
        {
            return;
        }

        var oldLanguage = currentLanguage;
        currentLanguage = normalized;
        LanguageChanged?.Invoke(this, new HelpLanguageChangedEventArgs(oldLanguage, normalized));
    }
}
