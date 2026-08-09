using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PeterSpoenemann.HelpService.Services;
using PeterSpoenemann.HelpService.ViewModels;
using Xunit;

namespace PeterSpoenemann.HelpService.Tests;

public sealed class MultilingualHelpServiceTests
{
    [Fact]
    public void RootHelpFileRemainsAliasForGermanConfiguration()
    {
        var options = new HelpServiceOptions();

        options.RootHelpFile = "custom-german.md";

        Assert.Equal("custom-german.md", options.RootHelpFiles[HelpLanguageCodes.German]);
        Assert.Equal(HelpLanguageCodes.German, options.Language);
    }

    [Fact]
    public void LegacySingleRootConfigurationDefaultsToGerman()
    {
        using var provider = CreateServices(includeEnglish: false);
        var languageService = provider.GetRequiredService<IHelpLanguageService>();
        var contentProvider = provider.GetRequiredService<IHelpContentProvider>();

        Assert.Equal(HelpLanguageCodes.German, languageService.CurrentLanguage);
        Assert.Equal([HelpLanguageCodes.German], languageService.SupportedLanguages);
        Assert.Equal("Einstellungen", contentProvider.GetTopic("settings").Title);
        Assert.Throws<ArgumentException>(() => languageService.SetLanguage(HelpLanguageCodes.English));
    }

    [Fact]
    public void RuntimeLanguageChangeUpdatesContentAndOpenViewModel()
    {
        using var provider = CreateServices(includeEnglish: true);
        var languageService = provider.GetRequiredService<IHelpLanguageService>();
        var contentProvider = provider.GetRequiredService<IHelpContentProvider>();
        var documentBuilder = provider.GetRequiredService<IHelpDocumentBuilder>();
        using var viewModel = new ContextHelpDialogViewModel(
            contentProvider,
            documentBuilder,
            "settings",
            languageService);
        HelpLanguageChangedEventArgs? eventArgs = null;
        languageService.LanguageChanged += (_, args) => eventArgs = args;

        Assert.Equal("Einstellungen", viewModel.Title);
        Assert.Equal("Hilfethemen", viewModel.HelpTopicsText);
        Assert.Contains("<html lang=\"de\">", viewModel.HtmlContent);

        languageService.SetLanguage("EN");

        Assert.Equal(HelpLanguageCodes.English, languageService.CurrentLanguage);
        Assert.Equal("Settings", contentProvider.GetTopic("settings").Title);
        Assert.Equal("Settings", viewModel.Title);
        Assert.Equal("Help topics", viewModel.HelpTopicsText);
        Assert.Equal("← Back", viewModel.BackButtonText);
        Assert.Contains("<html lang=\"en\">", viewModel.HtmlContent);
        Assert.Equal(HelpLanguageCodes.German, eventArgs?.OldLanguage);
        Assert.Equal(HelpLanguageCodes.English, eventArgs?.NewLanguage);
    }

    [Fact]
    public void EnglishProviderReturnsEnglishFallback()
    {
        var rootFile = Path.Combine(AppContext.BaseDirectory, "SampleHelp", "ContextHelp.en.md");
        var provider = new HelpContentProvider(
            rootFile,
            HelpLanguageCodes.English,
            NullLogger<HelpContentProvider>.Instance);

        var topic = provider.GetTopic("missing");

        Assert.Equal("Help", topic.Title);
        Assert.Contains("No dedicated help topic", topic.Markdown);
    }

    private static ServiceProvider CreateServices(bool includeEnglish)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<HelpContentProvider>>(NullLogger<HelpContentProvider>.Instance);
        services.AddPeterSpoenemannHelpService(options =>
        {
            options.RootHelpFile = Path.Combine(AppContext.BaseDirectory, "SampleHelp", "ContextHelp.de.md");
            if (includeEnglish)
            {
                options.RootHelpFiles[HelpLanguageCodes.English] =
                    Path.Combine(AppContext.BaseDirectory, "SampleHelp", "ContextHelp.en.md");
            }
        });
        return services.BuildServiceProvider();
    }
}
