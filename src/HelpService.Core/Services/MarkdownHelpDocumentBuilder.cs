using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Renderers;
using PeterSpoenemann.HelpService.Models;
using PeterSpoenemann.HelpService.Resources;

namespace PeterSpoenemann.HelpService.Services;

/// <summary>
/// Rendert Markdown mit den erweiterten Markdig-Funktionen als eigenständiges HTML-Dokument.
/// </summary>
public sealed class MarkdownHelpDocumentBuilder : IHelpDocumentBuilder, IHelpPageBuilder, IHelpThemeService
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();
    private static readonly string BaseStyleSheet = LoadStyleSheet("base");
    private static readonly string PageStyleSheet = LoadStyleSheet("page");
    private static readonly Regex IdAttributePattern = new(
        "id=\"(?<id>[^\"]+)\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LocalLinkPattern = new(
        "href=\"#(?<id>[^\"]+)\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TopicLinkPattern = new(
        "href=\"topic:(?<target>[^\"]+)\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly string? additionalCss;
    private HelpDocumentTheme currentTheme;

    /// <summary>
    /// Initialisiert den Renderer mit dem bisherigen hellen Standarddesign.
    /// </summary>
    public MarkdownHelpDocumentBuilder()
        : this(HelpDocumentTheme.Light)
    {
    }

    /// <summary>
    /// Initialisiert den Renderer mit einem eingebauten Theme und optionalen zusätzlichen CSS-Regeln.
    /// </summary>
    /// <param name="theme">Das in jedes HTML-Dokument eingebettete Theme.</param>
    /// <param name="additionalCss">
    /// Optionale CSS-Regeln, die nach Basis und Theme eingebettet werden und diese überschreiben können.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Das Theme ist nicht definiert.</exception>
    /// <exception cref="ArgumentException">
    /// Das zusätzliche CSS enthält ein schließendes <c>style</c>-Element.
    /// </exception>
    public MarkdownHelpDocumentBuilder(HelpDocumentTheme theme, string? additionalCss = null)
    {
        if (additionalCss?.Contains("</style", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new ArgumentException("Additional CSS must not contain a closing style element.", nameof(additionalCss));
        }

        ValidateTheme(theme);
        currentTheme = theme;
        this.additionalCss = additionalCss;
    }

    /// <inheritdoc />
    public event EventHandler<HelpThemeChangedEventArgs>? ThemeChanged;

    /// <inheritdoc />
    public HelpDocumentTheme CurrentTheme => currentTheme;

    /// <inheritdoc />
    public void SetTheme(HelpDocumentTheme theme)
    {
        ValidateTheme(theme);
        var oldTheme = currentTheme;
        if (oldTheme == theme)
        {
            return;
        }

        currentTheme = theme;
        ThemeChanged?.Invoke(this, new HelpThemeChangedEventArgs(oldTheme, theme));
    }

    /// <inheritdoc />
    public string BuildHtml(string markdown) => BuildHtml(markdown, HelpLanguageCodes.German);

    /// <inheritdoc />
    public string BuildHtml(string markdown, string language)
    {
        if (!HelpLanguageCodes.TryNormalize(language, out var normalizedLanguage))
        {
            throw new ArgumentException(
                HelpResources.Format("UnsupportedLanguage", HelpLanguageCodes.German, language),
                nameof(language));
        }

        var theme = currentTheme;
        var (styleSheet, colorScheme) = CreateStyleSheet(theme);
        var content = RenderMarkdown(markdown);

        return $$"""
            <!DOCTYPE html>
            <html lang="{{normalizedLanguage}}">
            <head>
              <meta charset="utf-8">
              <meta name="color-scheme" content="{{colorScheme}}">
              <style>
            {{styleSheet}}
              </style>
            </head>
            <body>{{content}}</body>
            </html>
            """;
    }

    /// <inheritdoc />
    public string BuildPageHtml(IReadOnlyList<HelpTopic> topics, string language, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(topics);
        if (topics.Count == 0)
        {
            throw new ArgumentException("At least one help topic is required.", nameof(topics));
        }

        if (!HelpLanguageCodes.TryNormalize(language, out var normalizedLanguage))
        {
            throw new ArgumentException(
                HelpResources.Format("UnsupportedLanguage", HelpLanguageCodes.German, language),
                nameof(language));
        }

        var anchors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var topic in topics)
        {
            if (string.IsNullOrWhiteSpace(topic.Id) || !anchors.TryAdd(topic.Id, CreateTopicAnchor(topic.Id)))
            {
                throw new ArgumentException($"Help topic IDs must be non-empty and unique: '{topic.Id}'.", nameof(topics));
            }
        }

        var documentTitle = string.IsNullOrWhiteSpace(title) ? GetDefaultPageTitle(normalizedLanguage) : title;
        var encodedTitle = HtmlEncoder.Default.Encode(documentTitle);
        var navigation = BuildNavigation(topics, anchors, encodedTitle);
        var sections = new StringBuilder();
        foreach (var topic in topics)
        {
            var anchor = anchors[topic.Id];
            var content = PrefixLocalAnchors(RenderMarkdown(topic.Markdown), anchor);
            content = RewriteTopicLinks(content, anchors);
            sections.AppendLine($"    <section class=\"help-topic\" id=\"{anchor}\">");
            sections.AppendLine($"      <h1>{HtmlEncoder.Default.Encode(topic.Title)}</h1>");
            sections.AppendLine(content);
            sections.AppendLine("    </section>");
        }

        var theme = currentTheme;
        var (styleSheet, colorScheme) = CreateStyleSheet(theme);
        return $$"""
            <!DOCTYPE html>
            <html lang="{{normalizedLanguage}}">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="color-scheme" content="{{colorScheme}}">
              <title>{{encodedTitle}}</title>
              <style>
            {{styleSheet}}
            {{PageStyleSheet}}
              </style>
            </head>
            <body class="help-page">
            {{navigation}}
              <main class="help-page-content">
            {{sections.ToString().TrimEnd()}}
              </main>
            </body>
            </html>
            """;
    }

    private static string RenderMarkdown(string markdown)
    {
        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer)
        {
            LinkRewriter = RewriteLocalImage,
        };
        Markdown.Convert(markdown, renderer, Pipeline);
        return writer.ToString();
    }

    private static string BuildNavigation(
        IReadOnlyList<HelpTopic> topics,
        IReadOnlyDictionary<string, string> anchors,
        string encodedTitle)
    {
        var navigation = new StringBuilder();
        navigation.AppendLine("  <nav class=\"help-page-navigation\" aria-label=\"Table of contents\">");
        navigation.AppendLine($"    <h1>{encodedTitle}</h1>");
        foreach (var group in topics.GroupBy(topic => topic.GroupName))
        {
            if (!string.IsNullOrWhiteSpace(group.Key))
            {
                navigation.AppendLine($"    <div class=\"help-page-group\">{HtmlEncoder.Default.Encode(group.Key)}</div>");
            }

            navigation.AppendLine("    <ul>");
            foreach (var topic in group)
            {
                navigation.AppendLine(
                    $"      <li><a href=\"#{anchors[topic.Id]}\">{HtmlEncoder.Default.Encode(topic.Title)}</a></li>");
            }
            navigation.AppendLine("    </ul>");
        }
        navigation.Append("  </nav>");
        return navigation.ToString();
    }

    private static string PrefixLocalAnchors(string html, string topicAnchor)
    {
        html = IdAttributePattern.Replace(html, match => $"id=\"{topicAnchor}--{match.Groups["id"].Value}\"");
        return LocalLinkPattern.Replace(html, match => $"href=\"#{topicAnchor}--{match.Groups["id"].Value}\"");
    }

    private static string RewriteTopicLinks(string html, IReadOnlyDictionary<string, string> anchors) =>
        TopicLinkPattern.Replace(html, match =>
        {
            var target = match.Groups["target"].Value.Trim('/');
            var fragmentIndex = target.IndexOf('#');
            var topicId = fragmentIndex < 0 ? target : target[..fragmentIndex];
            var fragment = fragmentIndex < 0 ? null : target[(fragmentIndex + 1)..];
            if (!anchors.TryGetValue(topicId, out var topicAnchor))
            {
                return match.Value;
            }

            var targetAnchor = string.IsNullOrWhiteSpace(fragment)
                ? topicAnchor
                : $"{topicAnchor}--{fragment}";
            return $"href=\"#{targetAnchor}\"";
        });

    private static string CreateTopicAnchor(string topicId) => $"help-topic-{Uri.EscapeDataString(topicId)}";

    private static string GetDefaultPageTitle(string language) => language switch
    {
        HelpLanguageCodes.English => "Help",
        HelpLanguageCodes.Polish => "Pomoc",
        _ => "Hilfe",
    };

    private (string StyleSheet, string ColorScheme) CreateStyleSheet(HelpDocumentTheme theme)
    {
        var themeName = theme switch
        {
            HelpDocumentTheme.Light => "light",
            HelpDocumentTheme.Dark => "dark",
            HelpDocumentTheme.System => "system",
            _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, "Unknown help document theme."),
        };
        var colorScheme = theme == HelpDocumentTheme.Light ? "light" : theme == HelpDocumentTheme.Dark ? "dark" : "light dark";
        var styleSheet = string.IsNullOrWhiteSpace(additionalCss)
            ? $"{BaseStyleSheet}{Environment.NewLine}{LoadStyleSheet(themeName)}"
            : $"{BaseStyleSheet}{Environment.NewLine}{LoadStyleSheet(themeName)}{Environment.NewLine}{additionalCss}";
        return (styleSheet, colorScheme);
    }

    private static void ValidateTheme(HelpDocumentTheme theme)
    {
        if (theme is not HelpDocumentTheme.Light and not HelpDocumentTheme.Dark and not HelpDocumentTheme.System)
        {
            throw new ArgumentOutOfRangeException(nameof(theme), theme, "Unknown help document theme.");
        }
    }

    private static string RewriteLocalImage(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && !uri.IsFile)
        {
            return url;
        }

        var extension = Path.GetExtension(url).ToLowerInvariant();
        var mimeType = extension switch
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

    private static string LoadStyleSheet(string name)
    {
        var resourceName = $"PeterSpoenemann.HelpService.Styles.{name}.css";
        using var stream = typeof(MarkdownHelpDocumentBuilder).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded style sheet '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Trim();
    }
}
