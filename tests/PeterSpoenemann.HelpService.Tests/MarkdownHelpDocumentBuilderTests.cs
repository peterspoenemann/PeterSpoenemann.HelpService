using PeterSpoenemann.HelpService.Models;
using PeterSpoenemann.HelpService.Services;
using Xunit;

namespace PeterSpoenemann.HelpService.Tests;

public sealed class MarkdownHelpDocumentBuilderTests
{
    [Fact]
    public void BuildsNavigablePageFromAllTopics()
    {
        HelpTopic[] topics =
        [
            new("settings", "Settings & options", "## Overview {#overview}\n\n[Local](#overview)\n\n[Report](topic:reports#details)", "Basics"),
            new("reports", "Reports", "## Details {#details}\n\nText", "Basics"),
        ];

        var html = new MarkdownHelpDocumentBuilder(HelpDocumentTheme.Dark)
            .BuildPageHtml(topics, HelpLanguageCodes.English, "Application help");

        Assert.Contains("<title>Application help</title>", html);
        Assert.Contains("class=\"help-page-navigation\"", html);
        Assert.Contains("href=\"#help-topic-settings\">Settings &amp; options</a>", html);
        Assert.Contains("id=\"help-topic-settings\"", html);
        Assert.Contains("id=\"help-topic-settings--overview\"", html);
        Assert.Contains("href=\"#help-topic-settings--overview\"", html);
        Assert.Contains("href=\"#help-topic-reports--details\"", html);
        Assert.DoesNotContain("href=\"topic:", html);
        Assert.Contains("content=\"dark\"", html);
    }

    [Fact]
    public void ThemeCanBeChangedAtRuntime()
    {
        var builder = new MarkdownHelpDocumentBuilder(HelpDocumentTheme.Light);
        HelpThemeChangedEventArgs? change = null;
        builder.ThemeChanged += (_, args) => change = args;

        builder.SetTheme(HelpDocumentTheme.Dark);
        var html = builder.BuildHtml("Text");

        Assert.Equal(HelpDocumentTheme.Dark, builder.CurrentTheme);
        Assert.Equal(new HelpThemeChangedEventArgs(HelpDocumentTheme.Light, HelpDocumentTheme.Dark), change);
        Assert.Contains("<meta name=\"color-scheme\" content=\"dark\">", html);
    }

    [Fact]
    public void BuildsCompleteHtmlAndDisablesRawHtml()
    {
        var builder = new MarkdownHelpDocumentBuilder();

        var html = builder.BuildHtml("## Überschrift\n\n**Wichtig**\n\n<script>alert('x')</script>");

        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<strong>Wichtig</strong>", html);
        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("data-source-file", html);
    }

    [Theory]
    [InlineData(HelpDocumentTheme.Light, "content=\"light\"", "--help-background: #ffffff")]
    [InlineData(HelpDocumentTheme.Dark, "content=\"dark\"", "--help-background: #1e2228")]
    [InlineData(HelpDocumentTheme.System, "content=\"light dark\"", "prefers-color-scheme: dark")]
    public void EmbedsSelectedTheme(
        HelpDocumentTheme theme,
        string expectedColorScheme,
        string expectedCss)
    {
        var html = new MarkdownHelpDocumentBuilder(theme).BuildHtml("Text");

        Assert.Contains(expectedColorScheme, html);
        Assert.Contains(expectedCss, html);
    }

    [Fact]
    public void AppendsCustomCssAndRejectsClosingStyleElement()
    {
        var html = new MarkdownHelpDocumentBuilder(
            HelpDocumentTheme.Light,
            "body { font-size: 18px; }").BuildHtml("Text");

        Assert.Contains("body { font-size: 18px; }", html);
        Assert.Throws<ArgumentException>(() => new MarkdownHelpDocumentBuilder(
            HelpDocumentTheme.Light,
            "</style><script>alert(1)</script>"));
    }
}
