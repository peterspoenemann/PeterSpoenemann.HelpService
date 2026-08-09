using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using PeterSpoenemann.HelpService.Resources;

namespace PeterSpoenemann.HelpService.Behaviors;

/// <summary>
/// Stellt angefügte WPF-Eigenschaften bereit, mit denen HTML-Inhalte in einem WebView2 angezeigt
/// und Links auf andere Hilfethemen an einen Befehl weitergereicht werden.
/// </summary>
public static class WebView2Html
{
    private const string TopicLinkPrefix = "topic:";

    private static Task<CoreWebView2Environment>? environmentTask;
    private static string applicationName = "PeterSpoenemann.HelpService";
    private static Func<string> getLanguage = () => HelpLanguageCodes.German;

    internal static void ConfigureApplicationName(string value)
    {
        applicationName = value;
        environmentTask = null;
    }

    internal static void ConfigureLanguage(Func<string> languageAccessor) => getLanguage = languageAccessor;

    /// <summary>
    /// Bezeichnet die angefügte Eigenschaft mit dem vollständig gerenderten HTML-Inhalt.
    /// </summary>
    public static readonly DependencyProperty ContentProperty = DependencyProperty.RegisterAttached(
        "Content",
        typeof(string),
        typeof(WebView2Html),
        new PropertyMetadata(string.Empty, OnContentChanged));

    /// <summary>
    /// Bezeichnet die angefügte Eigenschaft für den Befehl, der bei einem <c>topic:</c>-Link ausgeführt wird.
    /// </summary>
    public static readonly DependencyProperty TopicLinkCommandProperty = DependencyProperty.RegisterAttached(
        "TopicLinkCommand",
        typeof(ICommand),
        typeof(WebView2Html),
        new PropertyMetadata(null));

    private static readonly DependencyProperty NavigationHandlerAttachedProperty = DependencyProperty.RegisterAttached(
        "NavigationHandlerAttached",
        typeof(bool),
        typeof(WebView2Html),
        new PropertyMetadata(false));

    private static readonly DependencyPropertyKey ErrorMessagePropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "ErrorMessage",
        typeof(string),
        typeof(WebView2Html),
        new PropertyMetadata(string.Empty));

    /// <summary>
    /// Bezeichnet die schreibgeschützte angefügte Eigenschaft mit einer bei der WebView2-Initialisierung
    /// aufgetretenen Fehlermeldung.
    /// </summary>
    public static readonly DependencyProperty ErrorMessageProperty = ErrorMessagePropertyKey.DependencyProperty;

    /// <summary>
    /// Legt den anzuzeigenden HTML-Inhalt eines WPF-Elements fest.
    /// </summary>
    /// <param name="element">Das Element, für das der Inhalt gesetzt wird.</param>
    /// <param name="value">Das vollständige HTML-Dokument.</param>
    public static void SetContent(DependencyObject element, string value) => element.SetValue(ContentProperty, value);

    /// <summary>
    /// Gibt den für ein WPF-Element festgelegten HTML-Inhalt zurück.
    /// </summary>
    /// <param name="element">Das auszulesende Element.</param>
    /// <returns>Das vollständige HTML-Dokument.</returns>
    public static string GetContent(DependencyObject element) => (string)element.GetValue(ContentProperty);

    /// <summary>
    /// Legt den Befehl fest, der beim Öffnen eines Links mit dem Schema <c>topic:</c> ausgeführt wird.
    /// </summary>
    /// <param name="element">Das Element, für das der Befehl gesetzt wird.</param>
    /// <param name="value">Der Befehl; seine Befehlsparameter sind Themen-IDs.</param>
    public static void SetTopicLinkCommand(DependencyObject element, ICommand? value) =>
        element.SetValue(TopicLinkCommandProperty, value);

    /// <summary>
    /// Gibt den Befehl zurück, der Links auf andere Hilfethemen verarbeitet.
    /// </summary>
    /// <param name="element">Das auszulesende Element.</param>
    /// <returns>Der konfigurierte Befehl oder <see langword="null"/>.</returns>
    public static ICommand? GetTopicLinkCommand(DependencyObject element) =>
        (ICommand?)element.GetValue(TopicLinkCommandProperty);

    /// <summary>
    /// Gibt die bei der WebView2-Initialisierung aufgetretene Fehlermeldung zurück.
    /// </summary>
    /// <param name="element">Das auszulesende Element.</param>
    /// <returns>Die Fehlermeldung oder eine leere Zeichenfolge, wenn kein Fehler vorliegt.</returns>
    public static string GetErrorMessage(DependencyObject element) => (string)element.GetValue(ErrorMessageProperty);

    private static void OnContentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not WebView2 webView)
        {
            return;
        }

        webView.Loaded -= OnLoaded;
        webView.Loaded += OnLoaded;
        if (webView.IsLoaded)
        {
            _ = NavigateAsync(webView);
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is WebView2 webView)
        {
            _ = NavigateAsync(webView);
        }
    }

    private static async Task NavigateAsync(WebView2 webView)
    {
        try
        {
            environmentTask ??= CreateEnvironmentAsync();
            var environment = await environmentTask;
            await webView.EnsureCoreWebView2Async(environment);
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;

            // Nur einmal je WebView2-Instanz abonnieren - NavigateAsync laeuft bei jedem Themenwechsel erneut,
            // die zugrunde liegende CoreWebView2-Instanz bleibt dabei aber bestehen.
            if (!(bool)webView.GetValue(NavigationHandlerAttachedProperty))
            {
                webView.CoreWebView2.NavigationStarting += (_, args) => OnNavigationStarting(webView, args);
                webView.SetValue(NavigationHandlerAttachedProperty, true);
            }

            webView.NavigateToString(GetContent(webView));
            webView.SetValue(ErrorMessagePropertyKey, string.Empty);
            webView.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            webView.Visibility = Visibility.Collapsed;
            webView.SetValue(ErrorMessagePropertyKey,
                HelpResources.Format("WebViewError", getLanguage(), ex.Message));
        }
    }

    /// <summary>
    /// Links innerhalb der Hilfetexte auf andere Themen (Markdown-Syntax <c>[Titel](topic:themen-id)</c>) sollen
    /// im selben Hilfefenster das Zielthema anzeigen statt eine Chromium-Navigation zu versuchen, die für das
    /// per <c>NavigateToString</c> geladene Dokument ohnehin nicht sinnvoll waere. Echte http(s)-Links
    /// werden stattdessen im Standardbrowser geoeffnet, da das Hilfefenster keine Adressleiste/Navigation besitzt
    /// und der Anwender sonst darin "gefangen" waere.
    /// </summary>
    private static void OnNavigationStarting(WebView2 webView, CoreWebView2NavigationStartingEventArgs e)
    {
        if (TryGetTopicId(e.Uri, out var topicId))
        {
            e.Cancel = true;
            var command = GetTopicLinkCommand(webView);
            if (command?.CanExecute(topicId) == true)
            {
                command.Execute(topicId);
            }

            return;
        }

        if (IsExternalLink(e.Uri))
        {
            e.Cancel = true;
            OpenInDefaultBrowser(e.Uri);
        }
    }

    private static bool TryGetTopicId(string uri, out string topicId)
    {
        if (!uri.StartsWith(TopicLinkPrefix, StringComparison.OrdinalIgnoreCase))
        {
            topicId = string.Empty;
            return false;
        }

        topicId = uri[TopicLinkPrefix.Length..].Trim('/');
        return topicId.Length > 0;
    }

    private static bool IsExternalLink(string uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out var parsed)
        && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);

    private static void OpenInDefaultBrowser(string uri)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri) { UseShellExecute = true });
        }
        catch
        {
            // Kein Standardbrowser konfiguriert o. Ae. - der Klick bleibt dann folgenlos.
        }
    }

    private static Task<CoreWebView2Environment> CreateEnvironmentAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            applicationName, "WebView2");
        return CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
    }
}
