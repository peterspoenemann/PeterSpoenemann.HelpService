using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PeterSpoenemann.HelpService.Sample.Resources;
using PeterSpoenemann.HelpService.Services;

namespace PeterSpoenemann.HelpService.Sample;

public partial class MainWindow : Window
{
    private readonly IContextHelpService helpService;
    private IHelpLanguageService? languageService;
    private bool isApplyingLanguage;

    public MainWindow(IContextHelpService helpService, IHelpLanguageService languageService)
    {
        InitializeComponent();
        this.helpService = helpService;
        this.languageService = languageService;
        languageService.LanguageChanged += LanguageService_LanguageChanged;
        ApplyLanguage(languageService.CurrentLanguage);
    }

    private string CurrentTopicId =>
        SampleTabs.SelectedItem is TabItem { Tag: string topicId } ? topicId : "settings";

    private void HelpButton_Click(object sender, RoutedEventArgs e) => ShowCurrentHelp();

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F1)
        {
            return;
        }

        ShowCurrentHelp();
        e.Handled = true;
    }

    private void SampleTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReferenceEquals(e.Source, SampleTabs))
        {
            UpdateHelpButton();
        }
    }

    private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isApplyingLanguage
            || languageService is null
            || LanguageSelector.SelectedItem is not ComboBoxItem { Tag: string language })
        {
            return;
        }

        languageService.SetLanguage(language);
    }

    private void LanguageService_LanguageChanged(object? sender, HelpLanguageChangedEventArgs e) =>
        ApplyLanguage(e.NewLanguage);

    private void ShowCurrentHelp() => helpService.ShowHelp(CurrentTopicId, this);

    private void UpdateHelpButton()
    {
        if (HelpButton is not null && SampleTabs.SelectedItem is TabItem tab)
        {
            HelpButton.Content = SampleResources.Format(
                "HelpButtonFormat",
                languageService?.CurrentLanguage ?? HelpLanguageCodes.German,
                tab.Header);
        }
    }

    private void ApplyLanguage(string language)
    {
        isApplyingLanguage = true;
        try
        {
            Title = SampleResources.Get("WindowTitle", language);
            HelpHintText.Text = SampleResources.Get("HelpHint", language);
            ProductTitleText.Text = SampleResources.Get("ProductTitle", language);
            SubtitleText.Text = SampleResources.Get("Subtitle", language);
            LanguageLabel.Text = SampleResources.Get("LanguageLabel", language);
            GermanLanguageItem.Content = SampleResources.Get("GermanLanguage", language);
            EnglishLanguageItem.Content = SampleResources.Get("EnglishLanguage", language);
            PolishLanguageItem.Content = SampleResources.Get("PolishLanguage", language);
            SettingsTab.Header = SampleResources.Get("SettingsTab", language);
            ReportsTab.Header = SampleResources.Get("ReportsTab", language);
            SettingsHeadingText.Text = SampleResources.Get("SettingsHeading", language);
            DisplayNameLabel.Text = SampleResources.Get("DisplayNameLabel", language);
            if (string.IsNullOrEmpty(DisplayNameTextBox.Text)
                || DisplayNameTextBox.Text == SampleResources.Get("DefaultUser", HelpLanguageCodes.German)
                || DisplayNameTextBox.Text == SampleResources.Get("DefaultUser", HelpLanguageCodes.English)
                || DisplayNameTextBox.Text == SampleResources.Get("DefaultUser", HelpLanguageCodes.Polish))
            {
                DisplayNameTextBox.Text = SampleResources.Get("DefaultUser", language);
            }
            ColorSchemeLabel.Text = SampleResources.Get("ColorSchemeLabel", language);
            SystemThemeItem.Content = SampleResources.Get("SystemTheme", language);
            LightThemeItem.Content = SampleResources.Get("LightTheme", language);
            DarkThemeItem.Content = SampleResources.Get("DarkTheme", language);
            ReportsHeadingText.Text = SampleResources.Get("ReportsHeading", language);
            IncludeDetailsCheckBox.Content = SampleResources.Get("IncludeDetails", language);
            GeneratePreviewButton.Content = SampleResources.Get("GeneratePreview", language);
            ReportPlaceholderText.Text = SampleResources.Get("ReportPlaceholder", language);
            LanguageSelector.SelectedItem = language switch
            {
                HelpLanguageCodes.English => EnglishLanguageItem,
                HelpLanguageCodes.Polish => PolishLanguageItem,
                _ => GermanLanguageItem
            };
            UpdateHelpButton();
        }
        finally
        {
            isApplyingLanguage = false;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (languageService is not null)
        {
            languageService.LanguageChanged -= LanguageService_LanguageChanged;
        }

        base.OnClosed(e);
    }
}
