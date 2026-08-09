using System.Globalization;
using System.Resources;

namespace PeterSpoenemann.HelpService.Resources;

internal static class HelpResources
{
    private static readonly ResourceManager ResourceManager = new(
        "PeterSpoenemann.HelpService.Resources.Strings",
        typeof(HelpResources).Assembly);

    public static string Get(string key, string language)
    {
        var culture = new CultureInfo(language);
        return ResourceManager.GetString(key, culture)
            ?? throw new MissingManifestResourceException($"Missing HelpService resource '{key}' for '{language}'.");
    }

    public static string Format(string key, string language, params object?[] args) =>
        string.Format(CultureInfo.GetCultureInfo(language), Get(key, language), args);
}
