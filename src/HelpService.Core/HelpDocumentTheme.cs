namespace PeterSpoenemann.HelpService;

/// <summary>
/// Bestimmt das eingebaute Farbschema eines vom Core erzeugten Hilfe-HTML-Dokuments.
/// </summary>
public enum HelpDocumentTheme
{
    /// <summary>Helle Darstellung; entspricht dem bisherigen Standarddesign.</summary>
    Light,

    /// <summary>Dunkle Darstellung.</summary>
    Dark,

    /// <summary>Folgt über <c>prefers-color-scheme</c> der Systemeinstellung.</summary>
    System,
}
