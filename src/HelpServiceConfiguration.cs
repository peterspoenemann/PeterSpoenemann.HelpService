using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeterSpoenemann.HelpService.Behaviors;
using PeterSpoenemann.HelpService.Resources;
using PeterSpoenemann.HelpService.Services;

namespace PeterSpoenemann.HelpService;

/// <summary>
/// Stellt Erweiterungsmethoden zur Registrierung des HelpService in einem
/// <see cref="IServiceCollection"/>-Container bereit.
/// </summary>
public static class HelpServiceConfiguration
{
    /// <summary>
    /// Registriert die Dienste für Markdown-Inhalte und das kontextsensitive WPF-Hilfefenster.
    /// </summary>
    /// <param name="services">Der DI-Container, dem die HelpService-Dienste hinzugefügt werden.</param>
    /// <param name="configure">Optionale Aktion zum Konfigurieren von Hilfedatei und Anwendungsname.</param>
    /// <returns>Dieselbe <see cref="IServiceCollection"/>-Instanz für verkettete Registrierungen.</returns>
    /// <exception cref="InvalidOperationException">
    /// Wird ausgelöst, wenn kein Pfad zur Hilfe-Wurzeldatei oder kein Anwendungsname konfiguriert wurde.
    /// </exception>
    public static IServiceCollection AddPeterSpoenemannHelpService(
        this IServiceCollection services,
        Action<HelpServiceOptions>? configure = null)
    {
        var options = new HelpServiceOptions();
        configure?.Invoke(options);

        var messageLanguage = HelpLanguageCodes.TryNormalize(options.Language, out var initialLanguage)
            ? initialLanguage
            : HelpLanguageCodes.German;
        if (!HelpLanguageCodes.TryNormalize(options.Language, out initialLanguage))
        {
            throw new InvalidOperationException(
                HelpResources.Format("UnsupportedLanguage", messageLanguage, options.Language));
        }

        if (string.IsNullOrWhiteSpace(options.ApplicationName))
        {
            throw new InvalidOperationException(HelpResources.Get("ApplicationNameMissing", messageLanguage));
        }

        var rootHelpFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (language, path) in options.RootHelpFiles)
        {
            if (!HelpLanguageCodes.TryNormalize(language, out var normalizedLanguage))
            {
                throw new InvalidOperationException(
                    HelpResources.Format("UnsupportedLanguage", messageLanguage, language));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException(
                    HelpResources.Format("LanguageRootMissing", messageLanguage, normalizedLanguage));
            }

            rootHelpFiles[normalizedLanguage] = Path.IsPathFullyQualified(path)
                ? path
                : Path.Combine(AppContext.BaseDirectory, path);
        }

        if (!rootHelpFiles.ContainsKey(initialLanguage))
        {
            throw new InvalidOperationException(
                HelpResources.Format("LanguageRootMissing", messageLanguage, initialLanguage));
        }

        var languageService = new HelpLanguageService(rootHelpFiles.Keys, initialLanguage);
        WebView2Html.ConfigureApplicationName(options.ApplicationName);
        WebView2Html.ConfigureLanguage(() => languageService.CurrentLanguage);
        return services
            .AddSingleton(options)
            .AddSingleton<IHelpLanguageService>(languageService)
            .AddSingleton<IHelpContentProvider>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<HelpContentProvider>>();
                var providers = rootHelpFiles.ToDictionary(
                    entry => entry.Key,
                    entry => (IHelpContentProvider)new HelpContentProvider(entry.Value, entry.Key, logger),
                    StringComparer.OrdinalIgnoreCase);
                return new MultilingualHelpContentProvider(providers, languageService);
            })
            .AddSingleton<IHelpDocumentBuilder, MarkdownHelpDocumentBuilder>()
            .AddSingleton<IContextHelpService, ContextHelpService>();
    }
}
