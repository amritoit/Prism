using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Prism.Models;

namespace Prism.ViewModels;

/// <summary>
/// Observable wrapper around a chat message so streamed text updates the UI live.
/// </summary>
public partial class MessageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _content;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImage))]
    private Bitmap? _image;

    public MessageViewModel(ChatRole role, string content)
    {
        Role = role;
        _content = content;
    }

    public ChatRole Role { get; }

    public bool IsUser => Role == ChatRole.User;
    public bool IsAssistant => Role == ChatRole.Assistant;
    public bool HasImage => Image is not null;

    public string Header => Role switch
    {
        ChatRole.User => "You",
        ChatRole.Assistant => "Prism ✨",
        _ => "System"
    };

    public ChatMessage ToModel() => new(Role, Content);

    public void Append(string chunk) => Content += chunk;

    /// <summary>Raw bytes of the image, kept so the conversation can be persisted.</summary>
    public byte[]? ImageBytes { get; private set; }
    public string? ImageMime { get; private set; }

    /// <summary>Decodes raw image bytes into a displayable bitmap.</summary>
    public void SetImage(byte[] data, string? mime = null)
    {
        ImageBytes = data;
        ImageMime = mime;
        using var ms = new MemoryStream(data);
        Image = new Bitmap(ms);
    }
}
