using System.IO;
using Markdig;
using Markdig.Renderers;
using PeterSpoenemann.HelpService.Resources;

namespace PeterSpoenemann.HelpService.Services;

/// <summary>
/// Rendert Markdown mit den erweiterten Markdig-Funktionen als eigenständiges HTML-Dokument.
/// </summary>
public sealed class MarkdownHelpDocumentBuilder : IHelpDocumentBuilder
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

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

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer)
        {
            LinkRewriter = RewriteLocalImage,
        };
        Markdown.Convert(markdown, renderer, Pipeline);
        var content = writer.ToString();

        return $$"""
            <!DOCTYPE html>
            <html lang="{{normalizedLanguage}}">
            <head>
              <meta charset="utf-8">
              <meta name="color-scheme" content="light">
              <style>
                :root { font-family: "Segoe UI", sans-serif; color: #1f2937; background: #ffffff; }
                body { margin: 0; padding: 4px 22px 28px 4px; font-size: 15px; line-height: 1.55; }
                h1, h2, h3 { color: #174a7e; line-height: 1.25; }
                h2 { margin: 1.4em 0 .45em; font-size: 1.35rem; border-bottom: 1px solid #dbe3ec; padding-bottom: .25em; }
                h3 { margin: 1.2em 0 .35em; font-size: 1.1rem; }
                p { margin: 0 0 .8em; }
                ul, ol { padding-left: 1.6em; }
                li { margin: .2em 0; }
                table { width: 100%; border-collapse: collapse; margin: .8em 0 1.2em; }
                th, td { border: 1px solid #b4bec8; padding: 7px 9px; text-align: left; vertical-align: top; }
                th { background: #e8f0f8; color: #174a7e; }
                tr:nth-child(even) td { background: #f8fafc; }
                code { background: #f1f5f9; border-radius: 3px; padding: .12em .3em; }
                a { color: #0b63a8; }
                img { max-width: 100%; height: auto; }
                blockquote { margin: .8em 0 1em; padding: 8px 14px; color: #4b5563; border-left: 4px solid #cbd5e1; background: #f8fafc; }
                .markdown-alert { border: 1px solid; border-left-width: 5px; border-radius: 4px; padding: 11px 13px; margin: .8em 0 1em; }
                .markdown-alert > :last-child { margin-bottom: 0; }
                .markdown-alert-title { font-weight: 700; margin-bottom: .35em; }
                .markdown-alert-tip, .markdown-alert-note { background: #fff8e1; border-color: #e5b94e; }
                .markdown-alert-warning, .markdown-alert-important, .markdown-alert-caution { background: #fdecea; border-color: #d35947; }
                .task-list-item { list-style: none; }
                .footnotes { margin-top: 1.5em; border-top: 1px solid #dbe3ec; font-size: .92em; }
                @media print { body { padding: 0; } }
              </style>
            </head>
            <body>{{content}}</body>
            </html>
            """;
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
}
