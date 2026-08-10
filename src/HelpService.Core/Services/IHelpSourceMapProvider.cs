using PeterSpoenemann.HelpService.Models;

namespace PeterSpoenemann.HelpService.Services;

/// <summary>
/// Stellt für Werkzeugunterstützung die Herkunft der Zeilen eines aufgelösten Hilfethemas bereit.
/// </summary>
public interface IHelpSourceMapProvider
{
    /// <summary>
    /// Gibt zu jeder Zeile des von <see cref="IHelpContentProvider.GetTopic"/> gelieferten Markdowns
    /// die ursprüngliche Datei und Zeilennummer zurück.
    /// </summary>
    /// <param name="topicId">Die ID des geladenen Hilfethemas.</param>
    /// <returns>
    /// Die Quellzeilen in derselben Reihenfolge wie im aufgelösten Markdown oder eine leere Liste,
    /// wenn das Thema nicht existiert.
    /// </returns>
    IReadOnlyList<HelpSourceLine> GetSourceLines(string topicId);
}
