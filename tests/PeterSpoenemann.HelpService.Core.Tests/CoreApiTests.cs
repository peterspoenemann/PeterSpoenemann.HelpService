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

    [Fact]
    public void PreservesSourceLocationsAcrossNestedIncludes()
    {
        var topicsDirectory = Directory.CreateDirectory(Path.Combine(testDirectory, "Topics")).FullName;
        var sharedDirectory = Directory.CreateDirectory(Path.Combine(testDirectory, "Shared")).FullName;
        var rootFile = Path.Combine(testDirectory, "ContextHelp.de.md");
        var topicFile = Path.Combine(topicsDirectory, "Start.md");
        var sharedFile = Path.Combine(sharedDirectory, "Details.md");
        File.WriteAllText(rootFile, "!include Topics/Start.md");
        File.WriteAllText(topicFile, "# start | Start\n\n## Lokal\n\n!include ../Shared/Details.md");
        File.WriteAllText(sharedFile, "Gemeinsamer Text.\n\nNoch ein Absatz.");

        var provider = new HelpContentProvider(rootFile);
        var sourceLines = provider.GetSourceLines("start");

        Assert.Equal(provider.GetTopic("start").Markdown.Split(Environment.NewLine).Length, sourceLines.Count);
        Assert.Contains(sourceLines, line =>
            line.Text == "## Lokal" && line.FilePath == topicFile && line.LineNumber == 3);
        Assert.Contains(sourceLines, line =>
            line.Text == "Gemeinsamer Text." && line.FilePath == sharedFile && line.LineNumber == 1);
        Assert.Contains(sourceLines, line =>
            line.Text == "Noch ein Absatz." && line.FilePath == sharedFile && line.LineNumber == 3);
        Assert.Empty(provider.GetSourceLines("missing"));
    }

    public void Dispose() => Directory.Delete(testDirectory, recursive: true);
}
