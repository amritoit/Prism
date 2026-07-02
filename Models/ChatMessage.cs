namespace Prism.Models;

public enum ChatRole
{
    System,
    User,
    Assistant
}

/// <summary>
/// A single message in a conversation, provider-agnostic.
/// </summary>
public sealed record ChatMessage(ChatRole Role, string Content);
