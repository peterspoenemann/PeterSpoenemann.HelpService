using System.Windows;
using PeterSpoenemann.HelpService.ViewModels;

namespace PeterSpoenemann.HelpService.Dialogs;

/// <summary>
/// Zeigt Inhaltsverzeichnis, Suche, Navigation und den gerenderten Inhalt der Anwendungshilfe an.
/// </summary>
public partial class ContextHelpDialog : Window
{
    /// <summary>
    /// Initialisiert ein neues Hilfefenster mit dem angegebenen Ansichtsmodell.
    /// </summary>
    /// <param name="viewModel">Das Ansichtsmodell für Themenauswahl und Navigation.</param>
    public ContextHelpDialog(ContextHelpDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
