using System.IO;
using System.Text.RegularExpressions;
using PeterSpoenemann.HelpService.Models;
using PeterSpoenemann.HelpService.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PeterSpoenemann.HelpService.Services;

/// <summary>
/// Lädt Hilfethemen aus einer Markdown-Wurzeldatei und löst darin enthaltene Includes und Bildpfade auf.
/// </summary>
public sealed class HelpContentProvider : IHelpContentProvider
{
    private static readonly Regex IncludePattern = new(
        @"^\s*!include\s+(?:<(?<angle>[^>]+)>|""(?<quote>[^""]+)""|(?<plain>\S+))\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ImagePattern = new(
        @"!\[(?<alt>[^\]]*)\]\((?<path><[^>]+>|[^)\s]+)(?:\s+[""'][^""']*[""'])?\)",
        RegexOptions.Compiled);

    private readonly IReadOnlyDictionary<string, HelpTopic> _topics;
    private readonly IReadOnlyList<HelpTopic> _orderedTopics;
    private readonly string language;
    private readonly ILogger<HelpContentProvider> logger;

    /// <summary>
    /// Initialisiert einen Inhaltsanbieter für eine deutsche Hilfe-Wurzeldatei ohne verpflichtende
    /// Logging-Konfiguration.
    /// </summary>
    /// <param name="helpFilePath">Der vollständige oder relative Pfad zur Hilfe-Wurzeldatei.</param>
    public HelpContentProvider(string helpFilePath)
        : this(helpFilePath, HelpLanguageCodes.German, NullLogger<HelpContentProvider>.Instance)
    {
    }

    /// <summary>
    /// Initialisiert einen Inhaltsanbieter für eine bestimmte Sprache ohne verpflichtende Logging-Konfiguration.
    /// </summary>
    /// <param name="helpFilePath">Der vollständige oder relative Pfad zur Hilfe-Wurzeldatei.</param>
    /// <param name="language">Der Sprachcode <c>de</c>, <c>en</c> oder <c>pl</c>.</param>
    public HelpContentProvider(string helpFilePath, string language)
        : this(helpFilePath, language, NullLogger<HelpContentProvider>.Instance)
    {
    }

    /// <summary>
    /// Testbarer Konstruktor mit explizitem Wurzel-Dateipfad statt <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    /// <param name="helpFilePath">Der vollständige oder relative Pfad zur Hilfe-Wurzeldatei.</param>
    /// <param name="logger">Der Logger für Ladefehler, Warnungen und Statusinformationen.</param>
    public HelpContentProvider(string helpFilePath, ILogger<HelpContentProvider> logger)
        : this(helpFilePath, HelpLanguageCodes.German, logger)
    {
    }

    /// <summary>
    /// Initialisiert einen Inhaltsanbieter für eine bestimmte Sprache.
    /// </summary>
    /// <param name="helpFilePath">Der vollständige oder relative Pfad zur Hilfe-Wurzeldatei.</param>
    /// <param name="language">Der Sprachcode <c>de</c>, <c>en</c> oder <c>pl</c>.</param>
    /// <param name="logger">Der Logger für Ladefehler, Warnungen und Statusinformationen.</param>
    /// <exception cref="ArgumentException">Der Sprachcode wird nicht unterstützt.</exception>
    public HelpContentProvider(
        string helpFilePath,
        string language,
        ILogger<HelpContentProvider> logger)
    {
        if (!HelpLanguageCodes.TryNormalize(language, out this.language))
        {
            throw new ArgumentException(
                HelpResources.Format("UnsupportedLanguage", HelpLanguageCodes.German, language),
                nameof(language));
        }

        this.logger = logger;
        var helpRoot = Path.GetDirectoryName(Path.GetFullPath(helpFilePath))!;
        var orderedTopics = new List<HelpTopic>();
        _topics = LoadTopics(helpFilePath, helpRoot, orderedTopics);
        _orderedTopics = orderedTopics;
        logger.LogInformation(HelpResources.Get("TopicsLoaded", this.language), _topics.Count, helpFilePath);
    }

    /// <inheritdoc />
    public HelpTopic GetTopic(string topicId) => _topics.TryGetValue(topicId, out var topic)
        ? topic
        : new HelpTopic(
            topicId,
            HelpResources.Get("FallbackTitle", language),
            HelpResources.Get("FallbackMarkdown", language));

    /// <inheritdoc />
    public bool HasTopic(string topicId) => _topics.ContainsKey(topicId);

    /// <summary>
    /// Alle geladenen Themen in stabiler Ladereihenfolge (folgt der Reihenfolge der !include-Zeilen in der
    /// Wurzeldatei), für ein Inhaltsverzeichnis.
    /// </summary>
    /// <returns>Eine schreibgeschützte Liste der Themen.</returns>
    public IReadOnlyList<HelpTopic> GetAllTopics() => _orderedTopics;

    /// <summary>
    /// Lädt die Wurzeldatei fehlertolerant: Jede oberste <c>!include</c>-Zeile (und der restliche, nicht
    /// includierte Inhalt der Wurzeldatei) bildet eine eigene Fehlergrenze. Schlägt eine Einheit fehl (fehlende
    /// Datei, zirkuläres Include, Pfad außerhalb des Help-Ordners, ungültige Themenüberschrift), werden nur
    /// deren Themen übersprungen - alle anderen Themen bleiben verfügbar.
    /// </summary>
    private IReadOnlyDictionary<string, HelpTopic> LoadTopics(
        string rootFilePath, string helpRoot, List<HelpTopic> orderedTopics)
    {
        var topics = new Dictionary<string, HelpTopic>(StringComparer.OrdinalIgnoreCase);
        var topicSources = new Dictionary<string, (string File, int LineNumber)>(StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<string> rootLines;
        try
        {
            rootLines = File.ReadAllLines(rootFilePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HelpResources.Get("RootReadError", language), rootFilePath);
            return topics;
        }

        var inlineLines = new List<(string RawLine, int LineNumber)>();
        for (var lineNumber = 0; lineNumber < rootLines.Count; lineNumber++)
        {
            var rawLine = rootLines[lineNumber];
            var include = IncludePattern.Match(rawLine);
            if (!include.Success)
            {
                // Bild-Pfadauflösung wird bewusst NICHT hier ausgeführt, sondern verzögert in der eigenen
                // Fehlergrenze unten - ein fehlerhafter Bildpfad in einer Inline-Zeile darf nachfolgende
                // !include-Zeilen in dieser Schleife nicht verhindern.
                inlineLines.Add((rawLine, lineNumber + 1));
                continue;
            }

            var value = include.Groups.Cast<Group>().Skip(1).First(group => group.Success).Value;
            LoadUnit(
                () =>
                {
                    var includePath = ResolveRelativePath(rootFilePath, value, helpRoot);
                    return ReadExpandedLines(includePath, helpRoot, []);
                },
                topics, topicSources, orderedTopics, rootFilePath, lineNumber + 1);
        }

        if (inlineLines.Count > 0)
        {
            LoadUnit(
                () => inlineLines
                    .Select(l => new SourceLine(ResolveImagePaths(l.RawLine, rootFilePath, helpRoot), rootFilePath, l.LineNumber))
                    .ToList(),
                topics, topicSources, orderedTopics, rootFilePath, inlineLines[0].LineNumber);
        }

        return topics;
    }

    private void LoadUnit(
        Func<IReadOnlyList<SourceLine>> readLines,
        Dictionary<string, HelpTopic> topics,
        Dictionary<string, (string File, int LineNumber)> topicSources,
        List<HelpTopic> orderedTopics,
        string unitFile,
        int unitLineNumber)
    {
        try
        {
            var lines = readLines();
            ParseTopics(lines, topics, topicSources, orderedTopics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HelpResources.Get("IncludeLoadError", language),
                unitFile, unitLineNumber);
        }
    }

    private IReadOnlyList<SourceLine> ReadExpandedLines(
        string filePath,
        string helpRoot,
        HashSet<string> includeChain)
    {
        var fullPath = EnsureInsideHelpRoot(filePath, helpRoot);
        if (!includeChain.Add(fullPath))
        {
            throw new FormatException(HelpResources.Format("CyclicInclude", language, fullPath));
        }

        try
        {
            var result = new List<SourceLine>();
            var rawLines = File.ReadAllLines(fullPath);
            for (var lineNumber = 0; lineNumber < rawLines.Length; lineNumber++)
            {
                result.AddRange(ExpandLine(rawLines[lineNumber], fullPath, lineNumber + 1, helpRoot, includeChain));
            }

            return result;
        }
        finally
        {
            includeChain.Remove(fullPath);
        }
    }

    private IEnumerable<SourceLine> ExpandLine(
        string line,
        string sourceFile,
        int lineNumber,
        string helpRoot,
        HashSet<string> includeChain)
    {
        var include = IncludePattern.Match(line);
        if (include.Success)
        {
            var value = include.Groups.Cast<Group>().Skip(1).First(group => group.Success).Value;
            var includePath = ResolveRelativePath(sourceFile, value, helpRoot);
            return ReadExpandedLines(includePath, helpRoot, includeChain);
        }

        return [new SourceLine(ResolveImagePaths(line, sourceFile, helpRoot), sourceFile, lineNumber)];
    }

    private string ResolveImagePaths(string line, string sourceFile, string helpRoot)
    {
        return ImagePattern.Replace(line, match =>
        {
            var originalPath = match.Groups["path"].Value.Trim('<', '>');
            if (Uri.TryCreate(originalPath, UriKind.Absolute, out var uri) && !uri.IsFile)
            {
                return match.Value;
            }

            var imagePath = ResolveRelativePath(sourceFile, originalPath, helpRoot);
            return $"![{match.Groups["alt"].Value}](<{imagePath}>)";
        });
    }

    private string ResolveRelativePath(string sourceFile, string relativePath, string helpRoot)
    {
        var sourceDirectory = Path.GetDirectoryName(sourceFile)!;
        return EnsureInsideHelpRoot(Path.Combine(sourceDirectory, relativePath.Trim()), helpRoot);
    }

    private string EnsureInsideHelpRoot(string path, string helpRoot)
    {
        var fullPath = Path.GetFullPath(path);
        var relativePath = Path.GetRelativePath(Path.GetFullPath(helpRoot), fullPath);
        if (relativePath == ".." || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(HelpResources.Format("OutsideHelpRoot", language, path));
        }

        return fullPath;
    }

    private void ParseTopics(
        IReadOnlyList<SourceLine> lines,
        Dictionary<string, HelpTopic> topics,
        Dictionary<string, (string File, int LineNumber)> topicSources,
        List<HelpTopic> orderedTopics)
    {
        TopicBuilder? current = null;

        foreach (var line in lines.Append(new SourceLine("# __end__ | __end__", string.Empty, 0)))
        {
            if (line.Text.StartsWith("# ", StringComparison.Ordinal))
            {
                AddTopic(topics, topicSources, orderedTopics, current);
                current = ParseTopicHeading(line);
            }
            else if (current is not null)
            {
                current.Lines.Add(line.Text);
            }
        }
    }

    private TopicBuilder ParseTopicHeading(SourceLine line)
    {
        var heading = line.Text[2..];
        var separatorIndex = heading.IndexOf('|');
        if (separatorIndex <= 0 || separatorIndex == heading.Length - 1)
        {
            throw new FormatException(
                HelpResources.Format("InvalidHeading", language, line.File, line.LineNumber, heading));
        }

        return new TopicBuilder(heading[..separatorIndex].Trim(), heading[(separatorIndex + 1)..].Trim(), line.File, line.LineNumber);
    }

    private void AddTopic(
        Dictionary<string, HelpTopic> topics,
        Dictionary<string, (string File, int LineNumber)> topicSources,
        List<HelpTopic> orderedTopics,
        TopicBuilder? topic)
    {
        if (topic is null || topic.Id == "__end__")
        {
            return;
        }

        var groupName = string.IsNullOrEmpty(topic.File) ? string.Empty : Path.GetFileNameWithoutExtension(topic.File);
        var newTopic = new HelpTopic(
            topic.Id, topic.Title, string.Join(Environment.NewLine, topic.Lines).Trim(), groupName);
        if (!topics.TryAdd(topic.Id, newTopic))
        {
            var existing = topicSources[topic.Id];
            logger.LogWarning(HelpResources.Get("DuplicateTopic", language),
                topic.Id, topic.File, topic.LineNumber, existing.File, existing.LineNumber);
            return;
        }

        topicSources[topic.Id] = (topic.File, topic.LineNumber);
        orderedTopics.Add(newTopic);
    }

    private readonly record struct SourceLine(string Text, string File, int LineNumber);

    private sealed class TopicBuilder(string id, string title, string file, int lineNumber)
    {
        public string Id { get; } = id;
        public string Title { get; } = title;
        public string File { get; } = file;
        public int LineNumber { get; } = lineNumber;
        public List<string> Lines { get; } = [];
    }
}
