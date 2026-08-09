using System.Globalization;
using System.Resources;

namespace PeterSpoenemann.HelpService.Sample.Resources;

internal static class SampleResources
{
    private static readonly ResourceManager ResourceManager = new(
        "PeterSpoenemann.HelpService.Sample.Resources.Strings",
        typeof(SampleResources).Assembly);

    public static string Get(string key, string language) =>
        ResourceManager.GetString(key, CultureInfo.GetCultureInfo(language))
        ?? throw new MissingManifestResourceException($"Missing sample resource '{key}' for '{language}'.");

    public static string Format(string key, string language, params object?[] args) =>
        string.Format(CultureInfo.GetCultureInfo(language), Get(key, language), args);
}
