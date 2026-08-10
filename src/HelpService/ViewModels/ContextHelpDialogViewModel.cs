using System.Windows.Input;
using System.Windows;
using PeterSpoenemann.HelpService.Models;
using PeterSpoenemann.HelpService.Resources;
using PeterSpoenemann.HelpService.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PeterSpoenemann.HelpService.ViewModels;

/// <summary>
/// Repräsentiert einen auswählbaren Eintrag im Inhaltsverzeichnis.
/// </summary>
/// <param name="Id">Die Themen-ID.</param>
/// <param name="Title">Der Navigationstitel.</param>
/// <param name="IsCurrent">Gibt an, ob das Thema aktuell angezeigt wird.</param>
public sealed record HelpTopicItemViewModel(string Id, string Title, bool IsCurrent);

/// <summary>
/// Repräsentiert eine ein- oder ausklappbare Themengruppe im Inhaltsverzeichnis.
/// </summary>
/// <param name="Name">Der Gruppenname.</param>
/// <param name="Items">Die Themen der Gruppe.</param>
/// <param name="IsExpanded">Gibt an, ob die Gruppe aufgeklappt ist.</param>
/// <param name="ToggleExpandCommand">Der Befehl zum Umschalten des Aufklappzustands.</param>
public sealed record HelpTopicGroupViewModel(
    string Name, IReadOnlyList<HelpTopicItemViewModel> Items, bool IsExpanded, ICommand ToggleExpandCommand)
{
    /// <summary>
    /// Ruft den Gruppenname einschließlich eines Symbols für den aktuellen Aufklappzustand ab.
    /// </summary>
    public string HeaderText => (IsExpanded ? "▾ " : "▸ ") + Name;
}

/// <summary>
/// Steuert Themenauswahl, Suche sowie Vorwärts- und Rückwärtsnavigation des Hilfefensters.
/// </summary>
public sealed partial class ContextHelpDialogViewModel : ObservableObject, IDisposable
{
    private readonly IHelpContentProvider contentProvider;
    private readonly IHelpDocumentBuilder documentBuilder;
    private readonly IHelpLanguageService languageService;
    private readonly IHelpThemeService? themeService;
    private IReadOnlyList<HelpTopic> allTopics;
    private readonly Dictionary<string, bool> expandedGroups = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<string> backHistory = new();
    private readonly Stack<string> forwardHistory = new();
    private string currentTopicId;

    /// <summary>
    /// Ruft den Suchtext für die Filterung von Titeln und Inhalten ab oder legt ihn fest.
    /// </summary>
    [ObservableProperty]
    private string searchText = string.Empty;

    /// <summary>
    /// Ruft den Titel des aktuell angezeigten Themas ab oder legt ihn fest.
    /// </summary>
    [ObservableProperty]
    private string title = string.Empty;

    /// <summary>
    /// Ruft das gerenderte HTML des aktuell angezeigten Themas ab oder legt es fest.
    /// </summary>
    [ObservableProperty]
    private string htmlContent = string.Empty;

    /// <summary>
    /// Initialisiert das Ansichtsmodell und zeigt das angegebene Anfangsthema an.
    /// </summary>
    /// <param name="contentProvider">Die Quelle der geladenen Hilfethemen.</param>
    /// <param name="documentBuilder">Der Renderer für Markdown-Hilfedokumente.</param>
    /// <param name="initialTopicId">Die ID des zuerst anzuzeigenden Themas.</param>
    public ContextHelpDialogViewModel(
        IHelpContentProvider contentProvider,
        IHelpDocumentBuilder documentBuilder,
        string initialTopicId)
        : this(
            contentProvider,
            documentBuilder,
            initialTopicId,
            new HelpLanguageService([HelpLanguageCodes.German], HelpLanguageCodes.German))
    {
    }

    /// <summary>
    /// Initialisiert ein sprachumschaltbares Ansichtsmodell und zeigt das angegebene Anfangsthema an.
    /// </summary>
    /// <param name="contentProvider">Die sprachabhängige Quelle der geladenen Hilfethemen.</param>
    /// <param name="documentBuilder">Der Renderer für Markdown-Hilfedokumente.</param>
    /// <param name="initialTopicId">Die ID des zuerst anzuzeigenden Themas.</param>
    /// <param name="languageService">Der Dienst für die aktive Sprache und Laufzeitwechsel.</param>
    public ContextHelpDialogViewModel(
        IHelpContentProvider contentProvider,
        IHelpDocumentBuilder documentBuilder,
        string initialTopicId,
        IHelpLanguageService languageService)
    {
        this.contentProvider = contentProvider;
        this.documentBuilder = documentBuilder;
        this.languageService = languageService;
        themeService = documentBuilder as IHelpThemeService;
        allTopics = contentProvider.GetAllTopics();
        currentTopicId = initialTopicId;
        languageService.LanguageChanged += OnLanguageChanged;
        if (themeService is not null)
        {
            themeService.ThemeChanged += OnThemeChanged;
        }

        ShowTopic(initialTopicId, addToHistory: false);
    }

    /// <summary>
    /// Ruft den lokalisierten Fenstertitel ab.
    /// </summary>
    public string WindowTitle => HelpResources.Format("HelpWindowTitle", languageService.CurrentLanguage, Title);

    /// <summary>
    /// Ruft den lokalisierten Text für die Rückwärtsnavigation ab.
    /// </summary>
    public string BackButtonText => HelpResources.Get("BackButton", languageService.CurrentLanguage);

    /// <summary>
    /// Ruft den lokalisierten Text für die Vorwärtsnavigation ab.
    /// </summary>
    public string ForwardButtonText => HelpResources.Get("ForwardButton", languageService.CurrentLanguage);

    /// <summary>
    /// Ruft den lokalisierten Text für die Schließen-Schaltfläche ab.
    /// </summary>
    public string CloseButtonText => HelpResources.Get("CloseButton", languageService.CurrentLanguage);

    /// <summary>
    /// Ruft die lokalisierte Überschrift des Inhaltsverzeichnisses ab.
    /// </summary>
    public string HelpTopicsText => HelpResources.Get("HelpTopics", languageService.CurrentLanguage);

    /// <summary>
    /// Ruft den lokalisierten Hinweistext für das Suchfeld ab.
    /// </summary>
    public string SearchToolTip => HelpResources.Get("SearchToolTip", languageService.CurrentLanguage);

    /// <summary>
    /// Ruft das aktuell für die Hilfe verwendete Theme ab.
    /// </summary>
    public HelpDocumentTheme CurrentTheme => themeService?.CurrentTheme ?? HelpDocumentTheme.Light;

    /// <summary>
    /// Ruft die anhand des Suchtexts gefilterten und gruppierten Themen ab.
    /// </summary>
    public IEnumerable<HelpTopicGroupViewModel> Groups
    {
        get
        {
            var isSearching = !string.IsNullOrWhiteSpace(SearchText);
            var filtered = isSearching
                ? allTopics
                    .Where(t => t.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                        || t.Markdown.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList()
                : allTopics;

            return filtered
                .GroupBy(t => t.GroupName)
                .Select(g => new HelpTopicGroupViewModel(
                    g.Key,
                    g.Select(t => new HelpTopicItemViewModel(t.Id, t.Title, t.Id == currentTopicId)).ToList(),
                    // Während einer aktiven Suche immer aufgeklappt zeigen, damit Treffer sofort sichtbar sind.
                    isSearching || IsGroupExpanded(g.Key),
                    ToggleGroupCommand));
        }
    }

    partial void OnSearchTextChanged(string value) => OnPropertyChanged(nameof(Groups));

    partial void OnTitleChanged(string value) => OnPropertyChanged(nameof(WindowTitle));

    private bool IsGroupExpanded(string groupName) =>
        expandedGroups.TryGetValue(groupName, out var expanded) && expanded;

    /// <summary>
    /// Zeigt das angegebene Thema an und nimmt das bisherige Thema in die Rückwärtsnavigation auf.
    /// </summary>
    /// <param name="topicId">Die ID des anzuzeigenden Themas.</param>
    [RelayCommand]
    public void SelectTopic(string topicId) => ShowTopic(topicId, addToHistory: true);

    [RelayCommand]
    private void ToggleGroup(string groupName)
    {
        expandedGroups[groupName] = !IsGroupExpanded(groupName);
        OnPropertyChanged(nameof(Groups));
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack()
    {
        if (!backHistory.TryPop(out var topicId))
        {
            return;
        }

        forwardHistory.Push(currentTopicId);
        ShowTopic(topicId, addToHistory: false);
    }

    private bool CanGoBack() => backHistory.Count > 0;

    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private void GoForward()
    {
        if (!forwardHistory.TryPop(out var topicId))
        {
            return;
        }

        backHistory.Push(currentTopicId);
        ShowTopic(topicId, addToHistory: false);
    }

    private bool CanGoForward() => forwardHistory.Count > 0;

    private void ShowTopic(string topicId, bool addToHistory, bool forceRefresh = false)
    {
        if (!forceRefresh
            && string.Equals(topicId, currentTopicId, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(HtmlContent))
        {
            return;
        }

        if (addToHistory && !string.IsNullOrWhiteSpace(currentTopicId))
        {
            backHistory.Push(currentTopicId);
            forwardHistory.Clear();
        }

        var topic = contentProvider.GetTopic(topicId);
        currentTopicId = topicId;
        Title = topic.Title;
        HtmlContent = documentBuilder.BuildHtml(topic.Markdown, languageService.CurrentLanguage);
        OnPropertyChanged(nameof(Groups));
        GoBackCommand.NotifyCanExecuteChanged();
        GoForwardCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Hebt die Anmeldung am Sprachdienst auf.
    /// </summary>
    public void Dispose()
    {
        languageService.LanguageChanged -= OnLanguageChanged;
        if (themeService is not null)
        {
            themeService.ThemeChanged -= OnThemeChanged;
        }
    }

    private void OnThemeChanged(object? sender, HelpThemeChangedEventArgs e)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(RefreshTheme);
            return;
        }

        OnPropertyChanged(nameof(CurrentTheme));
        RefreshTheme();
    }

    private void RefreshTheme() => ShowTopic(currentTopicId, addToHistory: false, forceRefresh: true);

    private void OnLanguageChanged(object? sender, HelpLanguageChangedEventArgs e)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(RefreshLanguage);
            return;
        }

        RefreshLanguage();
    }

    private void RefreshLanguage()
    {
        allTopics = contentProvider.GetAllTopics();
        expandedGroups.Clear();
        ShowTopic(currentTopicId, addToHistory: false, forceRefresh: true);
        OnPropertyChanged(nameof(BackButtonText));
        OnPropertyChanged(nameof(ForwardButtonText));
        OnPropertyChanged(nameof(CloseButtonText));
        OnPropertyChanged(nameof(HelpTopicsText));
        OnPropertyChanged(nameof(SearchToolTip));
        OnPropertyChanged(nameof(WindowTitle));
    }
}
