using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Models;

namespace Prism.ViewModels;

/// <summary>
/// Sidebar entry representing one saved conversation.
/// </summary>
public partial class SessionViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtitle))]
    private string _title;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtitle))]
    private DateTime _updatedAt;

    private Action<SessionViewModel, string?>? _onMove;
    private Action<SessionViewModel>? _onDelete;

    public SessionViewModel(ChatSession session)
    {
        Session = session;
        _title = session.Title;
        _updatedAt = session.UpdatedAt;
    }

    /// <summary>The underlying persisted model.</summary>
    public ChatSession Session { get; }

    /// <summary>Entries shown in the "move to folder" flyout.</summary>
    public System.Collections.ObjectModel.ObservableCollection<SessionMoveTarget> MoveTargets { get; } = new();

    public void ConfigureMove(Action<SessionViewModel, string?> onMove) => _onMove = onMove;

    public void ConfigureDelete(Action<SessionViewModel> onDelete) => _onDelete = onDelete;

    [RelayCommand]
    private void Delete() => _onDelete?.Invoke(this);

    /// <summary>Rebuilds the move-to-folder menu from the given (folderId, label) pairs.</summary>
    public void SetMoveTargets(System.Collections.Generic.IEnumerable<(string? Id, string Label)> folders)
    {
        MoveTargets.Clear();
        foreach (var (id, label) in folders)
            MoveTargets.Add(new SessionMoveTarget(label, new RelayCommand(() => _onMove?.Invoke(this, id))));
    }

    public string Subtitle
    {
        get
        {
            var provider = string.IsNullOrEmpty(Session.ProviderId) ? "" : Session.ProviderId + " · ";
            return provider + Relative(UpdatedAt);
        }
    }

    private static string Relative(DateTime when)
    {
        var span = DateTime.Now - when;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        return when.ToString("MMM d");
    }
}

/// <summary>A single entry in the "move to folder" flyout.</summary>
public sealed class SessionMoveTarget
{
    public SessionMoveTarget(string label, IRelayCommand command)
    {
        Label = label;
        Command = command;
    }

    public string Label { get; }
    public IRelayCommand Command { get; }
}
