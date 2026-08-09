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
}
