using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Prism.Providers;

internal static class HttpResponseExtensions
{
    /// <summary>
    /// Like EnsureSuccessStatusCode, but includes the response body in the
    /// exception message so API errors (bad key, unknown model, quota) are visible.
    /// </summary>
    public static async Task EnsureSuccessOrThrowAsync(
        this HttpResponseMessage response,
        CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            body = string.Empty;
        }

        var detail = string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body.Trim();
        throw new ProviderException((int)response.StatusCode, detail ?? "Request failed");
    }
}

/// <summary>Raised when a provider returns a non-success HTTP response.</summary>
public sealed class ProviderException : System.Exception
{
    public int StatusCode { get; }

    public ProviderException(int statusCode, string detail)
        : base($"HTTP {statusCode}: {detail}")
    {
        StatusCode = statusCode;
    }
}
