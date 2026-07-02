using System;
using System.Collections.Generic;

namespace Prism.Models;

/// <summary>
/// A persisted conversation. Serialized to JSON on the local machine
/// (one file per session). Later this can be synced to a server.
/// </summary>
public sealed class ChatSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "New chat";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string? ProviderId { get; set; }
    public string? Model { get; set; }

    /// <summary>Id of the folder/project this session belongs to, or null if ungrouped.</summary>
    public string? ProjectId { get; set; }

    public List<StoredMessage> Messages { get; set; } = new();
}

/// <summary>A single persisted message within a <see cref="ChatSession"/>.</summary>
public sealed class StoredMessage
{
    /// <summary>"system", "user", or "assistant".</summary>
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;

    /// <summary>Optional generated image, stored as base64.</summary>
    public string? ImageBase64 { get; set; }
    public string? ImageMime { get; set; }
}
