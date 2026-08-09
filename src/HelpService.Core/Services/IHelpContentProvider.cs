using PeterSpoenemann.HelpService.Models;

namespace PeterSpoenemann.HelpService.Services;

/// <summary>
/// Stellt die aus den Markdown-Quelldateien geladenen Hilfethemen bereit.
/// </summary>
public interface IHelpContentProvider
{
    /// <summary>
    /// Gibt ein Hilfethema zurück und erzeugt für eine unbekannte ID ein Platzhalter-Thema.
    /// </summary>
    /// <param name="topicId">Die gesuchte Themen-ID.</param>
    /// <returns>Das geladene Thema oder ein Platzhalter-Thema.</returns>
    HelpTopic GetTopic(string topicId);

    /// <summary>
    /// Prüft, ob zu <paramref name="topicId"/> tatsächlich ein Hilfethema geladen wurde, ohne (anders als
    /// <see cref="GetTopic"/>) für unbekannte IDs ein Platzhalter-Thema zu erzeugen - genutzt, um z. B. einen
    /// Hilfe-Button für Seiten ohne Thema auszublenden.
    /// </summary>
    /// <param name="topicId">Die zu prüfende Themen-ID.</param>
    /// <returns><see langword="true"/>, wenn ein geladenes Thema mit dieser ID vorhanden ist.</returns>
    bool HasTopic(string topicId);

    /// <summary>
    /// Gibt alle geladenen Themen in stabiler Ladereihenfolge zurück.
    /// </summary>
    /// <returns>Eine schreibgeschützte Liste der Hilfethemen.</returns>
    IReadOnlyList<HelpTopic> GetAllTopics();
}
