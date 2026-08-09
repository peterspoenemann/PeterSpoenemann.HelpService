using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PeterSpoenemann.HelpService.Sample;

public partial class App : Application
{
    private ServiceProvider? serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());
        services.AddPeterSpoenemannHelpService(options =>
        {
            options.RootHelpFile = Path.Combine("Help", "ContextHelp.de.md");
            options.ApplicationName = "PeterSpoenemann.HelpService.Sample";
        });
        services.AddSingleton<MainWindow>();

        serviceProvider = services.BuildServiceProvider();
        MainWindow = serviceProvider.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
