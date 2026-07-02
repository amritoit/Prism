using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Prism.Models;

namespace Prism.Services;

/// <summary>
/// Persists chat sessions as individual JSON files under the user's
/// application-data folder. Swappable for a server-backed store later.
/// </summary>
public sealed class SessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _dir;

    public SessionStore()
    {
        _dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Prism", "sessions");
        Directory.CreateDirectory(_dir);
    }

    /// <summary>Loads all saved sessions, most recently updated first.</summary>
    public List<ChatSession> LoadAll()
    {
        var sessions = new List<ChatSession>();
        foreach (var file in Directory.EnumerateFiles(_dir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var session = JsonSerializer.Deserialize<ChatSession>(json, JsonOptions);
                if (session is not null)
                    sessions.Add(session);
            }
            catch (Exception)
            {
                // Skip corrupt session files.
            }
        }

        return sessions.OrderByDescending(s => s.UpdatedAt).ToList();
    }

    public void Save(ChatSession session)
    {
        try
        {
            var json = JsonSerializer.Serialize(session, JsonOptions);
            File.WriteAllText(PathFor(session.Id), json);
        }
        catch (Exception)
        {
            // Best-effort persistence.
        }
    }

    public void Delete(string id)
    {
        try
        {
            var path = PathFor(id);
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception)
        {
            // Ignore delete failures.
        }
    }

    private string PathFor(string id) => Path.Combine(_dir, id + ".json");
}
