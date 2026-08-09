using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeterSpoenemann.HelpService.Behaviors;
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
        if (string.IsNullOrWhiteSpace(options.RootHelpFile))
        {
            throw new InvalidOperationException("Für PeterSpoenemann.HelpService muss eine Hilfe-Wurzeldatei angegeben werden.");
        }

        if (string.IsNullOrWhiteSpace(options.ApplicationName))
        {
            throw new InvalidOperationException("Für PeterSpoenemann.HelpService muss ein Anwendungsname angegeben werden.");
        }

        WebView2Html.ConfigureApplicationName(options.ApplicationName);
        return services
            .AddSingleton(options)
            .AddSingleton<IHelpContentProvider>(provider =>
            {
                var rootFile = Path.IsPathFullyQualified(options.RootHelpFile)
                    ? options.RootHelpFile
                    : Path.Combine(AppContext.BaseDirectory, options.RootHelpFile);
                return new HelpContentProvider(
                    rootFile,
                    provider.GetRequiredService<ILogger<HelpContentProvider>>());
            })
            .AddSingleton<IHelpDocumentBuilder, MarkdownHelpDocumentBuilder>()
            .AddSingleton<IContextHelpService, ContextHelpService>();
    }
}
