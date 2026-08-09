namespace PeterSpoenemann.HelpService.Services;

/// <summary>
/// Erzeugt ein anzeigbares Hilfedokument aus einem Quelltextformat.
/// </summary>
public interface IHelpDocumentBuilder
{
    /// <summary>
    /// Wandelt Markdown in ein vollständiges HTML-Dokument um.
    /// </summary>
    /// <param name="markdown">Der zu rendernde Markdown-Inhalt.</param>
    /// <returns>Ein vollständiges HTML-Dokument.</returns>
    string BuildHtml(string markdown);

    /// <summary>
    /// Wandelt Markdown in ein vollständiges HTML-Dokument mit dem angegebenen Dokument-Sprachcode um.
    /// </summary>
    /// <param name="markdown">Der zu rendernde Markdown-Inhalt.</param>
    /// <param name="language">Der Sprachcode <c>de</c> oder <c>en</c>.</param>
    /// <returns>Ein vollständiges HTML-Dokument.</returns>
    /// <remarks>
    /// Die Standardimplementierung ruft aus Kompatibilitätsgründen <see cref="BuildHtml(string)"/> auf.
    /// Bestehende eigene Implementierungen müssen daher nicht angepasst werden.
    /// </remarks>
    string BuildHtml(string markdown, string language) => BuildHtml(markdown);
}
