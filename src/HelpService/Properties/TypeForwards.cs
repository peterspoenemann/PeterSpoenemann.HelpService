using System.Runtime.CompilerServices;
using PeterSpoenemann.HelpService.Models;
using PeterSpoenemann.HelpService.Services;

[assembly: TypeForwardedTo(typeof(PeterSpoenemann.HelpService.HelpLanguageCodes))]
[assembly: TypeForwardedTo(typeof(HelpTopic))]
[assembly: TypeForwardedTo(typeof(IHelpContentProvider))]
[assembly: TypeForwardedTo(typeof(HelpContentProvider))]
[assembly: TypeForwardedTo(typeof(IHelpDocumentBuilder))]
[assembly: TypeForwardedTo(typeof(MarkdownHelpDocumentBuilder))]
