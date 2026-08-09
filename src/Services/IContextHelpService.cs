using System.Windows;

namespace PeterSpoenemann.HelpService.Services;

/// <summary>
/// Öffnet und steuert das kontextsensitive Hilfefenster einer WPF-Anwendung.
/// </summary>
public interface IContextHelpService
{
    /// <summary>
    /// Zeigt das angeforderte Hilfethema an oder wechselt ein bereits geöffnetes Hilfefenster auf dieses Thema.
    /// </summary>
    /// <param name="topicId">Die ID des anzuzeigenden Hilfethemas.</param>
    /// <param name="owner">Das optionale Besitzerfenster; standardmäßig wird das Hauptfenster verwendet.</param>
    void ShowHelp(string topicId, Window? owner = null);
}
