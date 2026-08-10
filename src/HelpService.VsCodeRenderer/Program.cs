using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;
using PeterSpoenemann.HelpService;
using PeterSpoenemann.HelpService.Models;
using PeterSpoenemann.HelpService.Services;

return await PreviewRenderer.RunAsync(args);

internal static partial class PreviewRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var options = Options.Parse(args);
            var document = Path.GetFullPath(options.Document);
            var workspace = Path.GetFullPath(options.Workspace ?? Path.GetDirectoryName(document)!);
            var root = options.Root is null
                ? FindRoot(document, workspace, options.Language)
                : ResolvePath(options.Root, workspace);
            var language = NormalizeLanguage(options.Language ?? InferLanguage(root));
            var messages = new List<LogMessage>();
            var provider = new HelpContentProvider(root, language, new ListLogger(messages));
            var topics = provider.GetAllTopics()
                .Select(topic => new PreviewTopic(
                    topic.Id,
                    topic.Title,
                    RenderTopic(topic.Markdown, provider.GetSourceLines(topic.Id), Path.GetDirectoryName(root)!)))
                .ToArray();

            var response = new PreviewResponse(root, language, topics, messages);
            await Console.Out.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
            return 0;
        }
        catch (Exception exception)
        {
            await Console.Error.WriteAsync(exception.Message);
            return 1;
        }
    }

    private static string FindRoot(string document, string workspace, string? requestedLanguage)
    {
        if (RootFileName().IsMatch(Path.GetFileName(document)))
        {
            return document;
        }

        var workspaceWithSeparator = Path.TrimEndingDirectorySeparator(workspace) + Path.DirectorySeparatorChar;
        var directory = new DirectoryInfo(Path.GetDirectoryName(document)!);
        var candidates = new List<string>();
        while (directory is not null)
        {
            candidates.AddRange(directory.EnumerateFiles("ContextHelp*.md").Select(file => file.FullName));
            if (string.Equals(directory.FullName, Path.TrimEndingDirectorySeparator(workspace), StringComparison.OrdinalIgnoreCase)
                || !directory.FullName.StartsWith(workspaceWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            directory = directory.Parent;
        }

        var matching = candidates.Where(candidate => Includes(candidate, document, new HashSet<string>(StringComparer.OrdinalIgnoreCase))).ToList();
        if (matching.Count == 0)
        {
            throw new InvalidOperationException(
                $"Keine ContextHelp*.md-Wurzeldatei gefunden, die '{document}' einbindet. " +
                "Konfigurieren Sie helpService.preview.rootFile, falls die Datei außerhalb des üblichen Hilfeordners liegt.");
        }

        if (!string.IsNullOrWhiteSpace(requestedLanguage))
        {
            var normalized = NormalizeLanguage(requestedLanguage);
            var languageMatch = matching.FirstOrDefault(candidate => InferLanguage(candidate) == normalized);
            if (languageMatch is not null)
            {
                return languageMatch;
            }
        }

        return matching.FirstOrDefault(candidate => InferLanguage(candidate) == HelpLanguageCodes.German) ?? matching[0];
    }

    private static bool Includes(string source, string target, HashSet<string> visited)
    {
        source = Path.GetFullPath(source);
        if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!visited.Add(source) || !File.Exists(source))
        {
            return false;
        }

        foreach (var line in File.ReadLines(source))
        {
            var match = IncludeLine().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var value = match.Groups.Cast<Group>().Skip(1).First(group => group.Success).Value;
            var included = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, value.Trim()));
            if (Includes(included, target, visited))
            {
                return true;
            }
        }

        return false;
    }

    private static string ResolvePath(string path, string workspace) =>
        Path.GetFullPath(Path.IsPathFullyQualified(path) ? path : Path.Combine(workspace, path));

    private static string InferLanguage(string root)
    {
        var name = Path.GetFileNameWithoutExtension(root);
        var suffix = name.Split('.').LastOrDefault();
        return suffix is "en" or "pl" or "de" ? suffix : HelpLanguageCodes.German;
    }

    private static string NormalizeLanguage(string language) =>
        HelpLanguageCodes.TryNormalize(language, out var normalized)
            ? normalized
            : throw new ArgumentException($"Nicht unterstützter Sprachcode: {language}");

    private static string RenderTopic(
        string markdown,
        IReadOnlyList<HelpSourceLine> sourceLines,
        string helpRoot)
    {
        var document = Markdown.Parse(markdown, Pipeline);
        AnnotateBlocks(document, sourceLines, helpRoot);

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer)
        {
            LinkRewriter = RewriteLocalImage,
        };
        Pipeline.Setup(renderer);
        renderer.Render(document);
        writer.Flush();
        return writer.ToString();
    }

    private static void AnnotateBlocks(
        ContainerBlock container,
        IReadOnlyList<HelpSourceLine> sourceLines,
        string helpRoot)
    {
        foreach (var block in container)
        {
            if (block.Line >= 0 && block.Line < sourceLines.Count)
            {
                var source = sourceLines[block.Line];
                var displayPath = Path.GetRelativePath(helpRoot, source.FilePath)
                    .Replace(Path.DirectorySeparatorChar, '/');
                var attributes = block.GetAttributes();
                attributes.AddProperty("data-source-file", source.FilePath);
                attributes.AddProperty("data-source-line", source.LineNumber.ToString(CultureInfo.InvariantCulture));
                attributes.AddProperty("title", $"{displayPath}:{source.LineNumber}");
            }

            if (block is ContainerBlock childContainer)
            {
                AnnotateBlocks(childContainer, sourceLines, helpRoot);
            }
        }
    }

    private static string RewriteLocalImage(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && !uri.IsFile)
        {
            return url;
        }

        var mimeType = Path.GetExtension(url).ToLowerInvariant() switch
        {
            ".gif" => "image/gif",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            _ => null,
        };
        if (mimeType is null || !Path.IsPathFullyQualified(url) || !File.Exists(url))
        {
            return url;
        }

        var data = Convert.ToBase64String(File.ReadAllBytes(url));
        return $"data:{mimeType};base64,{data}";
    }

    [GeneratedRegex("""^\s*!include\s+(?:<([^>]+)>|"([^"]+)"|(\S+))\s*$""", RegexOptions.IgnoreCase)]
    private static partial Regex IncludeLine();

    [GeneratedRegex(@"^ContextHelp(?:\.[^.]+)?\.md$", RegexOptions.IgnoreCase)]
    private static partial Regex RootFileName();

    private sealed record Options(string Document, string? Workspace, string? Root, string? Language)
    {
        public static Options Parse(string[] args)
        {
            string? document = null;
            string? workspace = null;
            string? root = null;
            string? language = null;
            for (var index = 0; index < args.Length; index++)
            {
                var value = index + 1 < args.Length ? args[index + 1] : null;
                switch (args[index])
                {
                    case "--document": document = RequireValue(args[index], value); index++; break;
                    case "--workspace": workspace = RequireValue(args[index], value); index++; break;
                    case "--root": root = RequireValue(args[index], value); index++; break;
                    case "--language": language = RequireValue(args[index], value); index++; break;
                    default: throw new ArgumentException($"Unbekanntes Argument: {args[index]}");
                }
            }

            return new Options(document ?? throw new ArgumentException("--document fehlt."), workspace, root, language);
        }

        private static string RequireValue(string option, string? value) =>
            !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException($"Wert für {option} fehlt.");
    }

    private sealed record PreviewResponse(string RootFile, string Language, PreviewTopic[] Topics, List<LogMessage> Messages);
    private sealed record PreviewTopic(string Id, string Title, string Html);
    private sealed record LogMessage(string Level, string Message);

    private sealed class ListLogger(List<LogMessage> messages) : ILogger<HelpContentProvider>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                messages.Add(new LogMessage(logLevel.ToString(), formatter(state, exception)));
            }
        }
    }
}
