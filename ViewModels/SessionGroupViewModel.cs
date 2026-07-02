using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Prism.ViewModels;

/// <summary>
/// A sidebar group: either a folder/project or the "ungrouped" bucket,
/// containing the sessions that belong to it.
/// </summary>
public partial class SessionGroupViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isExpanded = true;

    public SessionGroupViewModel(string? projectId, string name, bool canDelete)
    {
        ProjectId = projectId;
        Name = name;
        CanDelete = canDelete;
    }

    /// <summary>Folder id, or null for the ungrouped bucket.</summary>
    public string? ProjectId { get; }

    public string Name { get; }

    /// <summary>False for the ungrouped bucket (which cannot be removed).</summary>
    public bool CanDelete { get; }

    public ObservableCollection<SessionViewModel> Sessions { get; } = new();

    public string CountLabel => Sessions.Count.ToString();
}
