using Microsoft.Extensions.Logging.Abstractions;
using PeterSpoenemann.HelpService.Services;
using Xunit;

namespace PeterSpoenemann.HelpService.Tests;

public sealed class SampleHelpContentTests
{
    [Fact]
    public void SampleHelpLoadsBothTopicsAndEmbedsItsImage()
    {
        var rootFile = Path.Combine(AppContext.BaseDirectory, "SampleHelp", "ContextHelp.de.md");
        var provider = new HelpContentProvider(rootFile, NullLogger<HelpContentProvider>.Instance);

        Assert.Equal(["settings", "reports"], provider.GetAllTopics().Select(topic => topic.Id));
        Assert.Contains("Tastaturbedienung", provider.GetTopic("settings").Markdown);
        Assert.Contains("topic:settings", provider.GetTopic("reports").Markdown);

        var html = new MarkdownHelpDocumentBuilder().BuildHtml(provider.GetTopic("settings").Markdown);
        Assert.Contains("data:image/svg+xml;base64,", html);
        Assert.Contains("markdown-alert-tip", html);
    }

    [Fact]
    public void EnglishSampleHelpLoadsBothTranslatedTopics()
    {
        var rootFile = Path.Combine(AppContext.BaseDirectory, "SampleHelp", "ContextHelp.en.md");
        var provider = new HelpContentProvider(
            rootFile,
            HelpLanguageCodes.English,
            NullLogger<HelpContentProvider>.Instance);

        Assert.Equal(["settings", "reports"], provider.GetAllTopics().Select(topic => topic.Id));
        Assert.Equal("Settings", provider.GetTopic("settings").Title);
        Assert.Contains("Keyboard controls", provider.GetTopic("reports").Markdown);

        var html = new MarkdownHelpDocumentBuilder().BuildHtml(
            provider.GetTopic("settings").Markdown,
            HelpLanguageCodes.English);
        Assert.Contains("<html lang=\"en\">", html);
        Assert.Contains("data:image/svg+xml;base64,", html);
    }

    [Fact]
    public void PolishSampleHelpLoadsBothTranslatedTopics()
    {
        var rootFile = Path.Combine(AppContext.BaseDirectory, "SampleHelp", "ContextHelp.pl.md");
        var provider = new HelpContentProvider(
            rootFile,
            HelpLanguageCodes.Polish,
            NullLogger<HelpContentProvider>.Instance);

        Assert.Equal(["settings", "reports"], provider.GetAllTopics().Select(topic => topic.Id));
        Assert.Equal("Ustawienia", provider.GetTopic("settings").Title);
        Assert.Contains("Obsługa klawiatury", provider.GetTopic("reports").Markdown);

        var html = new MarkdownHelpDocumentBuilder().BuildHtml(
            provider.GetTopic("settings").Markdown,
            HelpLanguageCodes.Polish);
        Assert.Contains("<html lang=\"pl\">", html);
        Assert.Contains("data:image/svg+xml;base64,", html);
    }
}
