using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PeterSpoenemann.HelpService.Services;

namespace PeterSpoenemann.HelpService.Sample;

public partial class MainWindow : Window
{
    private readonly IContextHelpService helpService;

    public MainWindow(IContextHelpService helpService)
    {
        InitializeComponent();
        this.helpService = helpService;
        UpdateHelpButton();
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

    private void ShowCurrentHelp() => helpService.ShowHelp(CurrentTopicId, this);

    private void UpdateHelpButton()
    {
        if (HelpButton is not null && SampleTabs.SelectedItem is TabItem tab)
        {
            HelpButton.Content = $"Hilfe zu „{tab.Header}“ (F1)";
        }
    }
}
