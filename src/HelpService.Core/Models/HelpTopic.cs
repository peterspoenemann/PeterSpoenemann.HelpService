namespace PeterSpoenemann.HelpService.Models;

/// <summary>
/// Beschreibt ein geladenes Hilfethema.
/// </summary>
/// <param name="Id">Die innerhalb der Anwendung eindeutige Themen-ID.</param>
/// <param name="Title">Der im Hilfefenster angezeigte Titel.</param>
/// <param name="Markdown">Der aufgelöste Markdown-Inhalt des Themas.</param>
/// <param name="GroupName">Der optionale Gruppenname für das Inhaltsverzeichnis.</param>
public sealed record HelpTopic(string Id, string Title, string Markdown, string GroupName = "");
