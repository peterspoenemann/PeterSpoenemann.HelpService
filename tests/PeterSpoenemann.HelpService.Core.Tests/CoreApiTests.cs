using PeterSpoenemann.HelpService.Models;
using PeterSpoenemann.HelpService.Services;
using Xunit;

namespace PeterSpoenemann.HelpService.Core.Tests;

public sealed class CoreApiTests : IDisposable
{
    private readonly string testDirectory = Path.Combine(
        Path.GetTempPath(), $"PeterSpoenemann.HelpService.Core.Tests-{Guid.NewGuid():N}");

    public CoreApiTests() => Directory.CreateDirectory(testDirectory);

    [Fact]
    public void LoadsTableOfContentsAndBuildsHtmlWithoutWpf()
    {
        var topicFile = Path.Combine(testDirectory, "Start.md");
        var rootFile = Path.Combine(testDirectory, "ContextHelp.en.md");
        File.WriteAllText(rootFile, "!include Start.md");
        File.WriteAllText(topicFile, "# start | Start\n\n## Welcome\n\nCore content.");

        var provider = new HelpContentProvider(rootFile, HelpLanguageCodes.English);
        var builder = new MarkdownHelpDocumentBuilder();

        var tableOfContents = provider.GetAllTopics();
        var html = builder.BuildHtml(provider.GetTopic("start").Markdown, HelpLanguageCodes.English);

        Assert.Collection(tableOfContents, topic =>
        {
            Assert.Equal("start", topic.Id);
            Assert.Equal("Start", topic.Title);
        });
        Assert.Contains("<html lang=\"en\">", html);
        Assert.Contains("Core content.", html);
    }

    [Fact]
    public void CoreAssemblyDoesNotReferenceWpf()
    {
        var assembly = typeof(HelpTopic).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.Equal("PeterSpoenemann.HelpService.Core", assembly.GetName().Name);
        Assert.DoesNotContain("PresentationFramework", references);
        Assert.DoesNotContain("PresentationCore", references);
        Assert.DoesNotContain("WindowsBase", references);
    }

    public void Dispose() => Directory.Delete(testDirectory, recursive: true);
}
