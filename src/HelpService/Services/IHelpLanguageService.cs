namespace PeterSpoenemann.HelpService.Services;

/// <summary>
/// Verwaltet die aktive Sprache der Hilfe und ermöglicht einen Sprachwechsel zur Laufzeit.
/// </summary>
public interface IHelpLanguageService
{
    /// <summary>
    /// Wird ausgelöst, nachdem die aktive Sprache gewechselt wurde.
    /// </summary>
    event EventHandler<HelpLanguageChangedEventArgs>? LanguageChanged;

    /// <summary>
    /// Ruft den aktuell aktiven Sprachcode ab.
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// Ruft die konfigurierten und zur Laufzeit verfügbaren Sprachcodes ab.
    /// </summary>
    IReadOnlyCollection<string> SupportedLanguages { get; }

    /// <summary>
    /// Wechselt die aktive Sprache und aktualisiert geöffnete Hilfefenster.
    /// </summary>
    /// <param name="language">Der Sprachcode <c>de</c>, <c>en</c> oder <c>pl</c>.</param>
    /// <exception cref="ArgumentException">Die Sprache wird nicht unterstützt oder wurde nicht konfiguriert.</exception>
    void SetLanguage(string language);
}

/// <summary>
/// Enthält die Sprachcodes vor und nach einem Sprachwechsel.
/// </summary>
/// <param name="oldLanguage">Die zuvor aktive Sprache.</param>
/// <param name="newLanguage">Die jetzt aktive Sprache.</param>
public sealed class HelpLanguageChangedEventArgs(string oldLanguage, string newLanguage) : EventArgs
{
    /// <summary>
    /// Ruft die zuvor aktive Sprache ab.
    /// </summary>
    public string OldLanguage { get; } = oldLanguage;

    /// <summary>
    /// Ruft die jetzt aktive Sprache ab.
    /// </summary>
    public string NewLanguage { get; } = newLanguage;
}
