using System.Collections.Generic;
using System.Threading;
using Prism.Models;

namespace Prism.Providers;

/// <summary>
/// Contract every AI backend implements. To add a new API tomorrow,
/// create a class that implements this interface and register it in
/// <see cref="App"/> where the <see cref="ProviderRegistry"/> is built.
/// </summary>
public interface IChatProvider
{
    /// <summary>Stable identifier used for settings storage (e.g. "openai").</summary>
    string Id { get; }

    /// <summary>Human friendly name shown in the UI (e.g. "OpenAI (ChatGPT)").</summary>
    string DisplayName { get; }

    /// <summary>URL where a user can obtain an API key.</summary>
    string ApiKeyUrl { get; }

    /// <summary>Suggested models. The UI combo box is editable so any model can be typed.</summary>
    IReadOnlyList<string> Models { get; }

    /// <summary>
    /// Streams the assistant response as incremental chunks (text and/or images).
    /// </summary>
    IAsyncEnumerable<ChatChunk> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        string model,
        string apiKey,
        CancellationToken cancellationToken);
}
