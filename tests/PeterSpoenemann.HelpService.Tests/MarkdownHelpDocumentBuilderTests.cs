using PeterSpoenemann.HelpService.Services;
using Xunit;

namespace PeterSpoenemann.HelpService.Tests;

public sealed class MarkdownHelpDocumentBuilderTests
{
    [Fact]
    public void BuildsCompleteHtmlAndDisablesRawHtml()
    {
        var builder = new MarkdownHelpDocumentBuilder();

        var html = builder.BuildHtml("## Überschrift\n\n**Wichtig**\n\n<script>alert('x')</script>");

        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<strong>Wichtig</strong>", html);
        Assert.DoesNotContain("<script>", html);
    }
}
