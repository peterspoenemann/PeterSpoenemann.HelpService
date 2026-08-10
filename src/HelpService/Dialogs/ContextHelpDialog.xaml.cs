using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using PeterSpoenemann.HelpService.ViewModels;
using DrawingColor = System.Drawing.Color;

namespace PeterSpoenemann.HelpService.Dialogs;

/// <summary>
/// Zeigt Inhaltsverzeichnis, Suche, Navigation und den gerenderten Inhalt der Anwendungshilfe an.
/// </summary>
public partial class ContextHelpDialog : Window
{
    private readonly ContextHelpDialogViewModel viewModel;

    /// <summary>
    /// Initialisiert ein neues Hilfefenster mit dem angegebenen Ansichtsmodell.
    /// </summary>
    /// <param name="viewModel">Das Ansichtsmodell für Themenauswahl und Navigation.</param>
    public ContextHelpDialog(ContextHelpDialogViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        ApplyTheme(viewModel.CurrentTheme);
        DataContext = viewModel;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        Closed += ContextHelpDialog_Closed;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ContextHelpDialogViewModel.CurrentTheme))
        {
            ApplyTheme(viewModel.CurrentTheme);
        }
    }

    private void ContextHelpDialog_Closed(object? sender, EventArgs e) =>
        viewModel.PropertyChanged -= ViewModel_PropertyChanged;

    private void ApplyTheme(HelpDocumentTheme theme)
    {
        var isDark = theme == HelpDocumentTheme.Dark
            || theme == HelpDocumentTheme.System && IsWindowsDarkTheme();

        if (theme == HelpDocumentTheme.System && SystemParameters.HighContrast)
        {
            Resources["HelpWindowBackground"] = SystemColors.WindowBrush;
            Resources["HelpControlBackground"] = SystemColors.ControlBrush;
            Resources["HelpForeground"] = SystemColors.WindowTextBrush;
            Resources["HelpHeading"] = SystemColors.HotTrackBrush;
            Resources["HelpMuted"] = SystemColors.GrayTextBrush;
            Resources["HelpBorder"] = SystemColors.ActiveBorderBrush;
            Resources["HelpBorderSubtle"] = SystemColors.InactiveBorderBrush;
            Resources["HelpSelectedBackground"] = SystemColors.HighlightBrush;
            Resources["HelpHoverBackground"] = SystemColors.ControlLightBrush;
            Resources["HelpErrorForeground"] = SystemColors.WindowTextBrush;
            HelpBrowserBackground = SystemColors.WindowColor;
        }
        else if (isDark)
        {
            SetPalette(
                "#1E2228", "#252B33", "#D6DBE3", "#82B9ED", "#B7C0CC",
                "#56616E", "#3B444F", "#293D50", "#303843", "#FF9A8D");
            HelpBrowserBackground = ColorConverter.ConvertFromString("#1E2228") is Color color
                ? color
                : Colors.Black;
        }
        else
        {
            SetPalette(
                "#FFFFFF", "#FFFFFF", "#1F2937", "#174A7E", "#4B5563",
                "#B4BEC8", "#DBE3EC", "#DCE6F1", "#EEF2F7", "#B22222");
            HelpBrowserBackground = Colors.White;
        }

        if (HelpBrowser is not null)
        {
            HelpBrowser.DefaultBackgroundColor = ToDrawingColor(HelpBrowserBackground);
        }
    }

    private Color HelpBrowserBackground { get; set; } = Colors.White;

    private void SetPalette(params string[] colors)
    {
        string[] keys =
        [
            "HelpWindowBackground", "HelpControlBackground", "HelpForeground", "HelpHeading", "HelpMuted",
            "HelpBorder", "HelpBorderSubtle", "HelpSelectedBackground", "HelpHoverBackground", "HelpErrorForeground"
        ];
        for (var index = 0; index < keys.Length; index++)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[index]));
            brush.Freeze();
            Resources[keys[index]] = brush;
        }
    }

    private static bool IsWindowsDarkTheme()
    {
        try
        {
            return Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                1) is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }

    private static DrawingColor ToDrawingColor(Color color) =>
        DrawingColor.FromArgb(color.A, color.R, color.G, color.B);
}
