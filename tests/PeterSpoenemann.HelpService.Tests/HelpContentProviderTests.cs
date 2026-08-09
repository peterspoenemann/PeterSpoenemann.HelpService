using Microsoft.Extensions.Logging.Abstractions;
using PeterSpoenemann.HelpService.Services;
using Xunit;

namespace PeterSpoenemann.HelpService.Tests;

public sealed class HelpContentProviderTests : IDisposable
{
    private readonly string testDirectory = Path.Combine(
        Path.GetTempPath(), $"PeterSpoenemann.HelpService.Tests-{Guid.NewGuid():N}");

    public HelpContentProviderTests() => Directory.CreateDirectory(testDirectory);

    [Fact]
    public void LoadsIncludedTopicsInDeclaredOrder()
    {
        var topicsDirectory = Directory.CreateDirectory(Path.Combine(testDirectory, "Topics")).FullName;
        File.WriteAllText(Path.Combine(testDirectory, "ContextHelp.de.md"),
            "!include Topics/Start.md\n!include Topics/Settings.md");
        File.WriteAllText(Path.Combine(topicsDirectory, "Start.md"),
            "# start | Start\n\nWillkommen.");
        File.WriteAllText(Path.Combine(topicsDirectory, "Settings.md"),
            "# settings | Einstellungen\n\nKonfiguration.");

        var provider = CreateProvider();

        Assert.Equal(["start", "settings"], provider.GetAllTopics().Select(topic => topic.Id));
        Assert.True(provider.HasTopic("START"));
        Assert.Equal("Willkommen.", provider.GetTopic("start").Markdown);
    }

    [Fact]
    public void ReturnsFallbackForUnknownTopic()
    {
        File.WriteAllText(Path.Combine(testDirectory, "ContextHelp.de.md"), string.Empty);

        var provider = CreateProvider();

        Assert.False(provider.HasTopic("missing"));
        Assert.Equal("Hilfe", provider.GetTopic("missing").Title);
    }

    private HelpContentProvider CreateProvider() => new(
        Path.Combine(testDirectory, "ContextHelp.de.md"),
        NullLogger<HelpContentProvider>.Instance);

    public void Dispose() => Directory.Delete(testDirectory, recursive: true);
}
