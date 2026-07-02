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
/// Google Gemini via the streamGenerateContent API (SSE).
/// Gemini has a free tier, making it a good default to try today.
/// </summary>
public sealed class GeminiProvider : IChatProvider
{
    private const string Base = "https://generativelanguage.googleapis.com/v1beta/models";
    private readonly HttpClient _http;

    public GeminiProvider(HttpClient http) => _http = http;

    public string Id => "gemini";
    public string DisplayName => "Google (Gemini)";
    public string ApiKeyUrl => "https://aistudio.google.com/app/apikey";

    public IReadOnlyList<string> Models { get; } = new[]
    {
        "gemini-3.5-flash",
        "gemini-2.5-flash",
        "gemini-2.0-flash",
        "gemini-2.0-flash-lite",
        "gemini-2.5-pro",
        "gemini-2.5-flash-image"
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

        var contents = messages
            .Where(m => m.Role != ChatRole.System)
            .Select(m => new
            {
                role = m.Role == ChatRole.Assistant ? "model" : "user",
                parts = new[] { new { text = m.Content } }
            });

        var payload = new Dictionary<string, object?>
        {
            ["contents"] = contents
        };
        if (!string.IsNullOrWhiteSpace(system))
        {
            payload["systemInstruction"] = new
            {
                parts = new[] { new { text = system } }
            };
        }

        // Image-generation models must be told to return the IMAGE modality.
        if (model.Contains("image", System.StringComparison.OrdinalIgnoreCase))
        {
            payload["generationConfig"] = new
            {
                responseModalities = new[] { "TEXT", "IMAGE" }
            };
        }
        else
        {
            // Give text models access to real-time info via Google Search grounding.
            payload["tools"] = new[]
            {
                new Dictionary<string, object?> { ["google_search"] = new { } }
            };
        }

        var url = $"{Base}/{model}:streamGenerateContent?alt=sse&key={apiKey}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };

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

            List<ChatChunk>? chunks = null;
            try
            {
                using var doc = JsonDocument.Parse(data);
                var parts = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts");

                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var t) &&
                        t.ValueKind == JsonValueKind.String)
                    {
                        var s = t.GetString();
                        if (!string.IsNullOrEmpty(s))
                            (chunks ??= new()).Add(ChatChunk.FromText(s));
                    }
                    else if (part.TryGetProperty("inlineData", out var inline) &&
                             inline.TryGetProperty("data", out var d) &&
                             d.ValueKind == JsonValueKind.String)
                    {
                        var mime = inline.TryGetProperty("mimeType", out var mt)
                            ? mt.GetString() ?? "image/png"
                            : "image/png";
                        var bytes = System.Convert.FromBase64String(d.GetString()!);
                        (chunks ??= new()).Add(ChatChunk.FromImage(bytes, mime));
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore malformed fragments.
            }
            catch (KeyNotFoundException)
            {
                // Safety/blocked responses may omit content parts.
            }
            catch (System.FormatException)
            {
                // Ignore invalid base64 image payloads.
            }

            if (chunks is not null)
            {
                foreach (var chunk in chunks)
                    yield return chunk;
            }
        }
    }
}
