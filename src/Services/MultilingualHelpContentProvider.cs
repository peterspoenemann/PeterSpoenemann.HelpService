using PeterSpoenemann.HelpService.Models;

namespace PeterSpoenemann.HelpService.Services;

internal sealed class MultilingualHelpContentProvider : IHelpContentProvider
{
    private readonly IReadOnlyDictionary<string, IHelpContentProvider> providers;
    private readonly IHelpLanguageService languageService;

    public MultilingualHelpContentProvider(
        IReadOnlyDictionary<string, IHelpContentProvider> providers,
        IHelpLanguageService languageService)
    {
        this.providers = providers;
        this.languageService = languageService;
    }

    public HelpTopic GetTopic(string topicId) => CurrentProvider.GetTopic(topicId);

    public bool HasTopic(string topicId) => CurrentProvider.HasTopic(topicId);

    public IReadOnlyList<HelpTopic> GetAllTopics() => CurrentProvider.GetAllTopics();

    private IHelpContentProvider CurrentProvider => providers[languageService.CurrentLanguage];
}
