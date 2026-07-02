using System.Net.Http;
using Prism.Providers;

namespace Prism.ViewModels;

/// <summary>
/// Provides a lightweight registry for the XAML designer's parameterless
/// constructor. Not used at runtime (see App.axaml.cs for the real wiring).
/// </summary>
internal static class DesignData
{
    public static ProviderRegistry Registry()
    {
        var http = new HttpClient();
        return new ProviderRegistry(new IChatProvider[]
        {
            new GeminiProvider(http),
            new OpenAIProvider(http),
            new AnthropicProvider(http)
        });
    }
}
