using System.IO;

namespace PeterSpoenemann.HelpService;

/// <summary>
/// Enthält die Einstellungen für die Registrierung und Laufzeit des HelpService.
/// </summary>
public sealed class HelpServiceOptions
{
    private const string DefaultRootHelpFile = "Help/ContextHelp.de.md";

    /// <summary>
    /// Initialisiert die Optionen mit einer deutschen Hilfe-Wurzeldatei und Deutsch als Standardsprache.
    /// </summary>
    public HelpServiceOptions()
    {
        RootHelpFiles[HelpLanguageCodes.German] = Path.Combine("Help", "ContextHelp.de.md");
    }

    /// <summary>
    /// Pfad zur Hilfe-Wurzeldatei. Relative Pfade werden gegen das Ausgabeverzeichnis
    /// der Anwendung aufgelöst.
    /// </summary>
    /// <remarks>
    /// Diese Eigenschaft bleibt aus Kompatibilitätsgründen erhalten und entspricht immer dem Eintrag
    /// <c>RootHelpFiles["de"]</c>.
    /// </remarks>
    public string RootHelpFile
    {
        get => RootHelpFiles.TryGetValue(HelpLanguageCodes.German, out var path)
            ? path
            : DefaultRootHelpFile;
        set => RootHelpFiles[HelpLanguageCodes.German] = value;
    }

    /// <summary>
    /// Ruft die Hilfe-Wurzeldateien nach Sprachcode ab. Unterstützt werden <c>de</c>, <c>en</c> und <c>pl</c>.
    /// Relative Pfade werden gegen das Ausgabeverzeichnis der Anwendung aufgelöst.
    /// </summary>
    public IDictionary<string, string> RootHelpFiles { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Ruft die beim Start aktive Sprache ab oder legt sie fest. Der Standardwert ist <c>de</c>.
    /// </summary>
    public string Language { get; set; } = HelpLanguageCodes.German;

    /// <summary>
    /// Unterordner unter <see cref="Environment.SpecialFolder.LocalApplicationData"/>
    /// für die WebView2-Benutzerdaten.
    /// </summary>
    public string ApplicationName { get; set; } = "PeterSpoenemann.HelpService";
}
