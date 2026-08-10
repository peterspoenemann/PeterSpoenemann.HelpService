namespace PeterSpoenemann.HelpService.Models;

/// <summary>
/// Ordnet eine Zeile des aufgelösten Themen-Markdowns ihrer ursprünglichen Quelldatei zu.
/// </summary>
/// <param name="Text">Der nach der Include- und Bildpfadverarbeitung verwendete Markdown-Text.</param>
/// <param name="FilePath">Der vollständige Pfad der ursprünglichen Markdown-Datei.</param>
/// <param name="LineNumber">Die einsbasierte Zeilennummer in der ursprünglichen Datei.</param>
public sealed record HelpSourceLine(string Text, string FilePath, int LineNumber);
