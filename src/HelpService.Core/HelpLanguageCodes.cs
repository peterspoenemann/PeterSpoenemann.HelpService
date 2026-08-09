namespace PeterSpoenemann.HelpService;

/// <summary>
/// Enthält die vom HelpService unterstützten Sprachcodes.
/// </summary>
public static class HelpLanguageCodes
{
    /// <summary>
    /// Der Sprachcode für Deutsch.
    /// </summary>
    public const string German = "de";

    /// <summary>
    /// Der Sprachcode für Englisch.
    /// </summary>
    public const string English = "en";

    /// <summary>
    /// Der Sprachcode für Polnisch.
    /// </summary>
    public const string Polish = "pl";

    /// <summary>
    /// Normalisiert einen unterstützten Sprachcode.
    /// </summary>
    /// <param name="language">Der zu prüfende Sprachcode.</param>
    /// <param name="normalized">Der normalisierte Sprachcode, sofern er unterstützt wird.</param>
    /// <returns><see langword="true"/>, wenn der Sprachcode unterstützt wird.</returns>
    public static bool TryNormalize(string? language, out string normalized)
    {
        normalized = language?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized is German or English or Polish;
    }
}
