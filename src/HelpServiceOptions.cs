using System.IO;

namespace PeterSpoenemann.HelpService;

/// <summary>
/// Enthält die Einstellungen für die Registrierung und Laufzeit des HelpService.
/// </summary>
public sealed class HelpServiceOptions
{
    /// <summary>
    /// Pfad zur Hilfe-Wurzeldatei. Relative Pfade werden gegen das Ausgabeverzeichnis
    /// der Anwendung aufgelöst.
    /// </summary>
    public string RootHelpFile { get; set; } = Path.Combine("Help", "ContextHelp.de.md");

    /// <summary>
    /// Unterordner unter <see cref="Environment.SpecialFolder.LocalApplicationData"/>
    /// für die WebView2-Benutzerdaten.
    /// </summary>
    public string ApplicationName { get; set; } = "PeterSpoenemann.HelpService";
}
