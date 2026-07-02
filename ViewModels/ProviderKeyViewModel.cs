using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Prism.ViewModels;

/// <summary>
/// Row in the settings panel: one editable API key per provider.
/// </summary>
public partial class ProviderKeyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _key;

    public ProviderKeyViewModel(string providerId, string displayName, string apiKeyUrl, string key)
    {
        ProviderId = providerId;
        DisplayName = displayName;
        ApiKeyUrl = apiKeyUrl;
        _key = key;
    }

    public string ProviderId { get; }
    public string DisplayName { get; }
    public string ApiKeyUrl { get; }

    /// <summary>Opens the provider's sign-in / API-key page in the default browser.</summary>
    [RelayCommand]
    private void OpenPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo(ApiKeyUrl) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // Ignore if no browser is available.
        }
    }
}
