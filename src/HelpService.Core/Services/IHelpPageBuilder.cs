using PeterSpoenemann.HelpService.Models;

namespace PeterSpoenemann.HelpService.Services;

/// <summary>
/// Erzeugt eine eigenständige HTML-Seite aus mehreren Hilfethemen.
/// </summary>
public interface IHelpPageBuilder
{
    /// <summary>
    /// Erzeugt eine vollständige HTML-Seite mit Inhaltsverzeichnis und einem Abschnitt je Hilfethema.
    /// </summary>
    /// <param name="topics">Die Themen in der gewünschten Reihenfolge.</param>
    /// <param name="language">Der Sprachcode <c>de</c>, <c>en</c> oder <c>pl</c>.</param>
    /// <param name="title">Ein optionaler Dokumenttitel.</param>
    /// <returns>Eine eigenständige HTML-Seite.</returns>
    string BuildPageHtml(IReadOnlyList<HelpTopic> topics, string language, string? title = null);
}
