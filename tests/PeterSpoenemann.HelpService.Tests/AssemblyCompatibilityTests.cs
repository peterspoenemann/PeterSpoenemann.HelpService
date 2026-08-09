using PeterSpoenemann.HelpService.Models;
using PeterSpoenemann.HelpService.Services;
using Xunit;

namespace PeterSpoenemann.HelpService.Tests;

public sealed class AssemblyCompatibilityTests
{
    [Fact]
    public void WpfAssemblyForwardsFormerPublicCoreTypes()
    {
        var forwardedTypes = typeof(HelpServiceConfiguration).Assembly.GetForwardedTypes();

        Assert.Contains(typeof(HelpLanguageCodes), forwardedTypes);
        Assert.Contains(typeof(HelpTopic), forwardedTypes);
        Assert.Contains(typeof(IHelpContentProvider), forwardedTypes);
        Assert.Contains(typeof(HelpContentProvider), forwardedTypes);
        Assert.Contains(typeof(IHelpDocumentBuilder), forwardedTypes);
        Assert.Contains(typeof(MarkdownHelpDocumentBuilder), forwardedTypes);
    }
}
