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
/// Anthropic Claude via the Messages API (streaming).
/// Claude keeps the system prompt separate and only allows user/assistant roles.
/// </summary>
public sealed class AnthropicProvider : IChatProvider
{
    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private readonly HttpClient _http;

    public AnthropicProvider(HttpClient http) => _http = http;

    public string Id => "anthropic";
    public string DisplayName => "Anthropic (Claude)";
    public string ApiKeyUrl => "https://console.anthropic.com/settings/keys";

    public IReadOnlyList<string> Models { get; } = new[]
    {
        "claude-3-5-haiku-latest",
        "claude-3-5-sonnet-latest",
        "claude-sonnet-4-latest",
        "claude-opus-4-latest"
    };

    public async IAsyncEnumerable<ChatChunk> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        string model,
        string apiKey,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var system = string.Join(
            "\n\n",
            messages.Where(m => m.Role == ChatRole.System).Select(m => m.Content));

        var turns = messages
            .Where(m => m.Role != ChatRole.System)
            .Select(m => new
            {
                role = m.Role == ChatRole.Assistant ? "assistant" : "user",
                content = m.Content
            });

        var payload = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["max_tokens"] = 4096,
            ["stream"] = true,
            ["messages"] = turns
        };
        if (!string.IsNullOrWhiteSpace(system))
            payload["system"] = system;

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

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
            if (data.Length == 0)
                continue;

            string? text = null;
            try
            {
                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;
                if (root.TryGetProperty("type", out var type) &&
                    type.GetString() == "content_block_delta" &&
                    root.TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("text", out var t) &&
                    t.ValueKind == JsonValueKind.String)
                {
                    text = t.GetString();
                }
            }
            catch (JsonException)
            {
                // Ignore non-JSON event framing lines.
            }

            if (!string.IsNullOrEmpty(text))
                yield return ChatChunk.FromText(text);
        }
    }
}
