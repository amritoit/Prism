using System.Collections.Generic;

namespace Prism.Models;

/// <summary>
/// Persisted user settings. API keys are stored per provider id.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Map of provider id -> API key.</summary>
    public Dictionary<string, string> ApiKeys { get; set; } = new();

    /// <summary>Last selected provider id.</summary>
    public string? LastProviderId { get; set; }

    /// <summary>Last selected model per provider id.</summary>
    public Dictionary<string, string> LastModel { get; set; } = new();

    /// <summary>Optional system prompt applied to every conversation.</summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>UI theme mode: "System", "Light" or "Dark".</summary>
    public string ThemeMode { get; set; } = "System";
}
