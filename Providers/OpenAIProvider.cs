using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using Prism.Models;

namespace Prism.Providers;

/// <summary>
/// OpenAI ChatGPT via the Chat Completions API (streaming).
/// </summary>
public sealed class OpenAIProvider : IChatProvider
{
    private const string Endpoint = "https://api.openai.com/v1/chat/completions";
    private readonly HttpClient _http;

    public OpenAIProvider(HttpClient http) => _http = http;

    public string Id => "openai";
    public string DisplayName => "OpenAI (ChatGPT)";
    public string ApiKeyUrl => "https://platform.openai.com/api-keys";

    public IReadOnlyList<string> Models { get; } = new[]
    {
        "gpt-4o-mini",
        "gpt-4o",
        "gpt-4.1-mini",
        "gpt-4.1",
        "o4-mini"
    };

    public async IAsyncEnumerable<ChatChunk> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        string model,
        string apiKey,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var payload = new
        {
            model,
            stream = true,
            messages = messages.Select(m => new
            {
                role = m.Role switch
                {
                    ChatRole.System => "system",
                    ChatRole.Assistant => "assistant",
                    _ => "user"
                },
                content = m.Content
            })
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        using var response = await _http
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        await response.EnsureSuccessOrThrowAsync(cancellationToken).ConfigureAwait(false);

        await using var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        await foreach (var line in Sse.ReadLinesAsync(stream, cancellationToken))
        {
            if (!line.StartsWith("data:", System.StringComparison.Ordinal))
                continue;

            var data = line["data:".Length..].Trim();
            if (data.Length == 0 || data == "[DONE]")
                continue;

            string? text = null;
            try
            {
                using var doc = JsonDocument.Parse(data);
                var delta = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("delta");
                if (delta.TryGetProperty("content", out var content) &&
                    content.ValueKind == JsonValueKind.String)
                {
                    text = content.GetString();
                }
            }
            catch (JsonException)
            {
                // Ignore malformed keep-alive fragments.
            }

            if (!string.IsNullOrEmpty(text))
                yield return ChatChunk.FromText(text);
        }
    }
}
