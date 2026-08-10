namespace PeterSpoenemann.HelpService.Services;

/// <summary>
/// Steuert das Theme neu erzeugter HTML-Hilfedokumente zur Laufzeit.
/// </summary>
public interface IHelpThemeService
{
    /// <summary>
    /// Wird ausgelöst, nachdem das aktive Theme geändert wurde.
    /// </summary>
    event EventHandler<HelpThemeChangedEventArgs>? ThemeChanged;

    /// <summary>
    /// Ruft das aktuell aktive Theme ab.
    /// </summary>
    HelpDocumentTheme CurrentTheme { get; }

    /// <summary>
    /// Aktiviert ein Theme für neu erzeugte Hilfedokumente.
    /// </summary>
    /// <param name="theme">Das zu aktivierende Theme.</param>
    void SetTheme(HelpDocumentTheme theme);
}

/// <summary>
/// Enthält das vorherige und das neue Hilfe-Theme.
/// </summary>
/// <param name="OldTheme">Das zuvor aktive Theme.</param>
/// <param name="NewTheme">Das jetzt aktive Theme.</param>
public sealed record HelpThemeChangedEventArgs(HelpDocumentTheme OldTheme, HelpDocumentTheme NewTheme);
