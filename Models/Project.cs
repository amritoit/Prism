using System;

namespace Prism.Models;

/// <summary>
/// A folder/project used to organize chat sessions in the sidebar.
/// Persisted locally; sessions reference it by <see cref="Id"/>.
/// </summary>
public sealed class Project
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New folder";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
