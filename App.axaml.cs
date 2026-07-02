using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.Net.Http;
using Avalonia.Markup.Xaml;
using Prism.Providers;
using Prism.Services;
using Prism.ViewModels;
using Prism.Views;

namespace Prism;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Single shared HttpClient for all providers.
            var http = new HttpClient();

            // Register every provider here. To add a new API tomorrow,
            // implement IChatProvider and add one line below.
            // The first entry is the default selection on a fresh install.
            var registry = new ProviderRegistry(new IChatProvider[]
            {
                new GeminiProvider(http),
                new OpenAIProvider(http),
                new AnthropicProvider(http)
            });

            var settings = new SettingsService();
            var sessionStore = new SessionStore();
            var projectStore = new ProjectStore();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(registry, settings, sessionStore, projectStore),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}