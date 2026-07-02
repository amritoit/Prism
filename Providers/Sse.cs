using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Prism.Providers;

/// <summary>
/// Minimal Server-Sent-Events helper: reads a response stream line by line.
/// Each provider interprets the lines according to its own protocol.
/// </summary>
internal static class Sse
{
    public static async IAsyncEnumerable<string> ReadLinesAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var reader = new StreamReader(stream);
        while (true)
        {
            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (line is null)
                yield break;
            yield return line;
        }
    }
}
