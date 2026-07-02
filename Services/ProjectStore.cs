using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Prism.Models;

namespace Prism.Services;

/// <summary>
/// Persists the list of folders/projects as a single JSON file under the
/// user's application-data folder. Swappable for a server-backed store later.
/// </summary>
public sealed class ProjectStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _path;

    public ProjectStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Prism");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "projects.json");
    }

    public List<Project> LoadAll()
    {
        try
        {
            if (!File.Exists(_path))
                return new List<Project>();

            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<Project>>(json, JsonOptions)
                   ?? new List<Project>();
        }
        catch (Exception)
        {
            return new List<Project>();
        }
    }

    public void SaveAll(IEnumerable<Project> projects)
    {
        try
        {
            var json = JsonSerializer.Serialize(projects, JsonOptions);
            File.WriteAllText(_path, json);
        }
        catch (Exception)
        {
            // Best-effort persistence.
        }
    }
}
