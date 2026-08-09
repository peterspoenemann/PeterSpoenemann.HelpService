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

    internal static bool TryNormalize(string? language, out string normalized)
    {
        normalized = language?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized is German or English;
    }
}
