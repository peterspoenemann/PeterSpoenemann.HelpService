using System.Windows;
using PeterSpoenemann.HelpService.Dialogs;
using PeterSpoenemann.HelpService.ViewModels;

namespace PeterSpoenemann.HelpService.Services;

/// <summary>
/// Standardimplementierung des kontextsensitiven WPF-Hilfefensters.
/// </summary>
public sealed class ContextHelpService : IContextHelpService
{
    private readonly IHelpContentProvider contentProvider;
    private readonly IHelpDocumentBuilder documentBuilder;
    private readonly IHelpLanguageService languageService;
    private ContextHelpDialog? dialog;
    private ContextHelpDialogViewModel? viewModel;

    /// <summary>
    /// Initialisiert den Dienst mit Themenquelle und Dokumentrenderer.
    /// </summary>
    /// <param name="contentProvider">Die Quelle der geladenen Hilfethemen.</param>
    /// <param name="documentBuilder">Der Renderer für Markdown-Hilfedokumente.</param>
    public ContextHelpService(IHelpContentProvider contentProvider, IHelpDocumentBuilder documentBuilder)
        : this(
            contentProvider,
            documentBuilder,
            new HelpLanguageService([HelpLanguageCodes.German], HelpLanguageCodes.German))
    {
    }

    /// <summary>
    /// Initialisiert den Dienst mit Themenquelle, Dokumentrenderer und Laufzeit-Sprachdienst.
    /// </summary>
    /// <param name="contentProvider">Die sprachabhängige Quelle der geladenen Hilfethemen.</param>
    /// <param name="documentBuilder">Der Renderer für Markdown-Hilfedokumente.</param>
    /// <param name="languageService">Der Dienst für die aktive Sprache und Laufzeitwechsel.</param>
    public ContextHelpService(
        IHelpContentProvider contentProvider,
        IHelpDocumentBuilder documentBuilder,
        IHelpLanguageService languageService)
    {
        this.contentProvider = contentProvider;
        this.documentBuilder = documentBuilder;
        this.languageService = languageService;
    }

    /// <inheritdoc />
    public void ShowHelp(string topicId, Window? owner = null)
    {
        var requestedOwner = owner ?? System.Windows.Application.Current.MainWindow;
        HelpWindowPlacement? placement = null;
        if (dialog is not null && viewModel is not null)
        {
            if (dialog.IsEnabled && ReferenceEquals(dialog.Owner, requestedOwner))
            {
                viewModel.SelectTopic(topicId);
                if (dialog.WindowState == WindowState.Minimized)
                {
                    dialog.WindowState = WindowState.Normal;
                }

                dialog.Activate();
                return;
            }

            // ShowDialog deaktiviert alle Fenster, die beim Öffnen des modalen Dialogs bereits existieren.
            // Eine solche Hilfe muss nach dem F1-Aufruf mit dem modalen Dialog als Owner neu erzeugt werden.
            placement = HelpWindowPlacement.From(dialog);
            dialog.Close();
        }

        var newViewModel = new ContextHelpDialogViewModel(
            contentProvider,
            documentBuilder,
            topicId,
            languageService);
        var newDialog = new ContextHelpDialog(newViewModel)
        {
            Owner = requestedOwner
        };
        placement?.ApplyTo(newDialog);
        viewModel = newViewModel;
        dialog = newDialog;
        newDialog.Closed += (_, _) =>
        {
            newViewModel.Dispose();
            if (ReferenceEquals(dialog, newDialog))
            {
                dialog = null;
                viewModel = null;
            }
        };
        newDialog.Show();
    }

    private readonly record struct HelpWindowPlacement(Rect Bounds, WindowState State)
    {
        public static HelpWindowPlacement From(Window window)
        {
            var bounds = window.RestoreBounds;
            if (bounds.IsEmpty)
            {
                bounds = new Rect(window.Left, window.Top, window.Width, window.Height);
            }

            var state = window.WindowState == WindowState.Minimized
                ? WindowState.Normal
                : window.WindowState;
            return new HelpWindowPlacement(bounds, state);
        }

        public void ApplyTo(Window window)
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = Bounds.Left;
            window.Top = Bounds.Top;
            window.Width = Bounds.Width;
            window.Height = Bounds.Height;
            window.WindowState = State;
        }
    }
}
