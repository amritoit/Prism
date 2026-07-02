namespace Prism.Providers;

/// <summary>
/// A single streamed piece of an assistant response: either a text fragment
/// or an inline image (e.g. from an image-generation model).
/// </summary>
public readonly record struct ChatChunk(string? Text, byte[]? ImageData, string? ImageMime)
{
    public static ChatChunk FromText(string text) => new(text, null, null);

    public static ChatChunk FromImage(byte[] data, string mime) => new(null, data, mime);

    public bool IsImage => ImageData is not null;
}
