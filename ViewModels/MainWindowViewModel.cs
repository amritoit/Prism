using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Models;
using Prism.Providers;
using Prism.Services;

namespace Prism.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public const string CustomModelOption = "Custom model…";

    private readonly ProviderRegistry _registry;
    private readonly SettingsService _settingsService;
    private readonly SessionStore _sessionStore;
    private readonly ProjectStore _projectStore;
    private readonly AppSettings _settings;
    private readonly List<Project> _projects;
    private CancellationTokenSource? _cts;

    private ChatSession? _currentSession;
    private SessionViewModel? _currentSessionVm;
    private bool _suppressSessionLoad;

    public MainWindowViewModel()
        : this(DesignData.Registry(), new SettingsService(), new SessionStore(), new ProjectStore())
    {
        // Parameterless ctor for the XAML designer only.
    }

    public MainWindowViewModel(
        ProviderRegistry registry,
        SettingsService settingsService,
        SessionStore sessionStore,
        ProjectStore projectStore)
    {
        _registry = registry;
        _settingsService = settingsService;
        _sessionStore = sessionStore;
        _projectStore = projectStore;
        _settings = settingsService.Load();
        _projects = _projectStore.LoadAll();

        Providers = new ObservableCollection<IChatProvider>(_registry.All);
        Models = new ObservableCollection<string>();
        Messages = new ObservableCollection<MessageViewModel>();
        ProviderKeys = new ObservableCollection<ProviderKeyViewModel>(
            _registry.All.Select(p => new ProviderKeyViewModel(
                p.Id, p.DisplayName, p.ApiKeyUrl,
                _settings.ApiKeys.TryGetValue(p.Id, out var k) ? k : string.Empty)));

        Sessions = new ObservableCollection<SessionViewModel>(
            _sessionStore.LoadAll().Select(CreateSessionVm));
        Groups = new ObservableCollection<SessionGroupViewModel>();

        RefreshMoveTargets();
        RebuildGroups();

        SystemPrompt = _settings.SystemPrompt;

        var initial = Providers.FirstOrDefault(p => p.Id == _settings.LastProviderId)
                      ?? Providers.FirstOrDefault();
        SelectedProvider = initial;
    }

    public ObservableCollection<IChatProvider> Providers { get; }
    public ObservableCollection<string> Models { get; }
    public ObservableCollection<MessageViewModel> Messages { get; }
    public ObservableCollection<ProviderKeyViewModel> ProviderKeys { get; }
    public ObservableCollection<SessionViewModel> Sessions { get; }
    public ObservableCollection<SessionGroupViewModel> Groups { get; }

    [ObservableProperty]
    private IChatProvider? _selectedProvider;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomModel))]
    private string? _selectedModel;

    [ObservableProperty]
    private string _customModelText = string.Empty;

    [ObservableProperty]
    private object? _selectedNode;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isSearchVisible;

    [ObservableProperty]
    private string _newProjectName = string.Empty;

    [ObservableProperty]
    private bool _isNewFolderVisible;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string _input = string.Empty;

    [ObservableProperty]
    private string _systemPrompt = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private string? _statusMessage;

    public bool IsIdle => !IsBusy;
    public bool IsCustomModel => SelectedModel == CustomModelOption;

    partial void OnSelectedProviderChanged(IChatProvider? value)
    {
        Models.Clear();
        if (value is null)
            return;

        foreach (var m in value.Models)
            Models.Add(m);
        Models.Add(CustomModelOption);

        SelectedModel = _settings.LastModel.TryGetValue(value.Id, out var last)
                        && value.Models.Contains(last)
            ? last
            : value.Models.FirstOrDefault();
    }

    partial void OnSelectedProviderChanged(IChatProvider? oldValue, IChatProvider? newValue)
        => SendCommand.NotifyCanExecuteChanged();

    partial void OnSelectedNodeChanged(object? value)
    {
        if (_suppressSessionLoad)
            return;
        if (value is SessionViewModel svm)
            LoadSession(svm);
    }

    partial void OnSearchTextChanged(string value) => RebuildGroups();

    [RelayCommand]
    private void ToggleSearch()
    {
        IsSearchVisible = !IsSearchVisible;
        if (!IsSearchVisible)
            SearchText = string.Empty;
    }

    [RelayCommand]
    private void ToggleNewFolder() => IsNewFolderVisible = !IsNewFolderVisible;

    [RelayCommand]
    private void ToggleSettings() => IsSettingsOpen = !IsSettingsOpen;

    [RelayCommand]
    private void SaveSettings()
    {
        foreach (var row in ProviderKeys)
            _settings.ApiKeys[row.ProviderId] = row.Key.Trim();

        _settings.SystemPrompt = SystemPrompt;
        _settingsService.Save(_settings);
        IsSettingsOpen = false;
        StatusMessage = "Settings saved.";
    }

    [RelayCommand]
    private void NewChat()
    {
        _suppressSessionLoad = true;
        SelectedNode = null;
        _suppressSessionLoad = false;

        _currentSession = null;
        _currentSessionVm = null;
        Messages.Clear();
        StatusMessage = null;
    }

    [RelayCommand]
    private void DeleteSession(SessionViewModel? session)
    {
        if (session is null)
            return;

        _sessionStore.Delete(session.Session.Id);
        Sessions.Remove(session);

        var wasCurrent = ReferenceEquals(_currentSessionVm, session);
        RebuildGroups();

        if (wasCurrent)
            NewChat();
    }

    [RelayCommand]
    private void AddProject()
    {
        var name = NewProjectName?.Trim();
        if (string.IsNullOrEmpty(name))
            return;

        _projects.Add(new Project { Name = name });
        _projectStore.SaveAll(_projects);
        NewProjectName = string.Empty;
        IsNewFolderVisible = false;

        RefreshMoveTargets();
        RebuildGroups();
    }

    [RelayCommand]
    private void DeleteProject(SessionGroupViewModel? group)
    {
        if (group?.ProjectId is null)
            return;

        _projects.RemoveAll(p => p.Id == group.ProjectId);
        _projectStore.SaveAll(_projects);

        // Detach any sessions that were in the removed folder.
        foreach (var s in Sessions.Where(s => s.Session.ProjectId == group.ProjectId))
        {
            s.Session.ProjectId = null;
            _sessionStore.Save(s.Session);
        }

        RefreshMoveTargets();
        RebuildGroups();
    }

    private void MoveSessionToProject(SessionViewModel session, string? projectId)
    {
        session.Session.ProjectId = projectId;
        _sessionStore.Save(session.Session);
        RebuildGroups();
    }

    private SessionViewModel CreateSessionVm(ChatSession session)
    {
        var vm = new SessionViewModel(session);
        vm.ConfigureMove(MoveSessionToProject);
        vm.ConfigureDelete(s => DeleteSession(s));
        return vm;
    }

    private void RefreshMoveTargets()
    {
        var targets = new List<(string?, string)> { (null, "✕  No folder") };
        foreach (var p in _projects.OrderBy(p => p.Name))
            targets.Add((p.Id, "📁  " + p.Name));

        foreach (var s in Sessions)
            s.SetMoveTargets(targets);
    }

    private void RebuildGroups()
    {
        Groups.Clear();
        var q = SearchText?.Trim();
        var hasQuery = !string.IsNullOrEmpty(q);

        bool Match(SessionViewModel s) =>
            !hasQuery || s.Title.Contains(q!, StringComparison.OrdinalIgnoreCase);

        foreach (var p in _projects.OrderBy(p => p.Name))
        {
            var items = Sessions
                .Where(s => s.Session.ProjectId == p.Id && Match(s))
                .OrderByDescending(s => s.UpdatedAt)
                .ToList();

            if (hasQuery && items.Count == 0)
                continue;

            var g = new SessionGroupViewModel(p.Id, p.Name, canDelete: true);
            foreach (var s in items)
                g.Sessions.Add(s);
            Groups.Add(g);
        }

        var ungrouped = Sessions
            .Where(s => string.IsNullOrEmpty(s.Session.ProjectId) && Match(s))
            .OrderByDescending(s => s.UpdatedAt)
            .Take(100)
            .ToList();

        if (ungrouped.Count > 0 || _projects.Count == 0)
        {
            var g = new SessionGroupViewModel(null, "Recents", canDelete: false);
            foreach (var s in ungrouped)
                g.Sessions.Add(s);
            Groups.Add(g);
        }
    }

    private void LoadSession(SessionViewModel vm)
    {
        _currentSession = vm.Session;
        _currentSessionVm = vm;
        StatusMessage = null;

        Messages.Clear();
        foreach (var sm in vm.Session.Messages)
        {
            var role = sm.Role switch
            {
                "system" => ChatRole.System,
                "assistant" => ChatRole.Assistant,
                _ => ChatRole.User
            };
            var mvm = new MessageViewModel(role, sm.Content);
            if (!string.IsNullOrEmpty(sm.ImageBase64))
            {
                try { mvm.SetImage(Convert.FromBase64String(sm.ImageBase64), sm.ImageMime); }
                catch (FormatException) { /* ignore bad image data */ }
            }
            Messages.Add(mvm);
        }

        // Restore provider + model for the session.
        var provider = Providers.FirstOrDefault(p => p.Id == vm.Session.ProviderId);
        if (provider is not null)
            SelectedProvider = provider;

        var model = vm.Session.Model;
        if (!string.IsNullOrEmpty(model))
        {
            if (Models.Contains(model))
            {
                SelectedModel = model;
            }
            else
            {
                SelectedModel = CustomModelOption;
                CustomModelText = model;
            }
        }
    }

    private bool CanSend => IsIdle && !string.IsNullOrWhiteSpace(Input) && SelectedProvider is not null;

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        var provider = SelectedProvider;
        if (provider is null)
            return;

        var model = ResolveModel(provider);

        var apiKey = _settings.ApiKeys.TryGetValue(provider.Id, out var k) ? k : string.Empty;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            StatusMessage = $"No API key set for {provider.DisplayName}. Open Settings to add one.";
            IsSettingsOpen = true;
            return;
        }

        var userText = Input.Trim();
        Input = string.Empty;
        StatusMessage = null;

        Messages.Add(new MessageViewModel(ChatRole.User, userText));
        var assistant = new MessageViewModel(ChatRole.Assistant, string.Empty);
        Messages.Add(assistant);

        var history = BuildHistory();

        IsBusy = true;
        _cts = new CancellationTokenSource();
        try
        {
            await foreach (var chunk in provider.StreamAsync(history, model, apiKey, _cts.Token))
            {
                if (chunk.IsImage)
                    assistant.SetImage(chunk.ImageData!, chunk.ImageMime);
                else if (!string.IsNullOrEmpty(chunk.Text))
                    assistant.Append(chunk.Text!);
            }

            if (string.IsNullOrEmpty(assistant.Content) && !assistant.HasImage)
                assistant.Content = "(no response)";

            // Persist last used provider/model on a successful exchange.
            _settings.LastProviderId = provider.Id;
            _settings.LastModel[provider.Id] = model;
            _settingsService.Save(_settings);
        }
        catch (OperationCanceledException)
        {
            assistant.Append(assistant.Content.Length == 0 ? "(stopped)" : "\n\n(stopped)");
        }
        catch (ProviderException ex)
        {
            assistant.Content = $"Error: {ex.Message}";
            StatusMessage = ex.Message;
        }
        catch (Exception ex)
        {
            assistant.Content = $"Error: {ex.Message}";
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;

            PersistCurrentSession(provider.Id, model);
        }
    }

    [RelayCommand]
    private void Stop() => _cts?.Cancel();

    private string ResolveModel(IChatProvider provider)
    {
        if (IsCustomModel)
        {
            var custom = CustomModelText?.Trim();
            if (!string.IsNullOrEmpty(custom))
                return custom;
        }

        return string.IsNullOrWhiteSpace(SelectedModel)
            ? provider.Models.FirstOrDefault() ?? string.Empty
            : SelectedModel!.Trim();
    }

    private void PersistCurrentSession(string providerId, string model)
    {
        if (Messages.Count == 0)
            return;

        var stored = new List<StoredMessage>();
        foreach (var m in Messages)
        {
            if (m.Role == ChatRole.Assistant && string.IsNullOrEmpty(m.Content) && !m.HasImage)
                continue;

            var sm = new StoredMessage
            {
                Role = m.Role switch
                {
                    ChatRole.System => "system",
                    ChatRole.Assistant => "assistant",
                    _ => "user"
                },
                Content = m.Content
            };
            if (m.HasImage && m.ImageBytes is not null)
            {
                sm.ImageBase64 = Convert.ToBase64String(m.ImageBytes);
                sm.ImageMime = m.ImageMime;
            }
            stored.Add(sm);
        }

        var isNew = _currentSession is null;
        if (isNew)
        {
            _currentSession = new ChatSession();
            _currentSessionVm = CreateSessionVm(_currentSession);
            Sessions.Insert(0, _currentSessionVm);
            RefreshMoveTargets();
        }

        _currentSession!.Messages = stored;
        _currentSession.ProviderId = providerId;
        _currentSession.Model = model;
        _currentSession.UpdatedAt = DateTime.Now;

        var firstUser = Messages.FirstOrDefault(x => x.Role == ChatRole.User);
        if (firstUser is not null)
            _currentSession.Title = Truncate(firstUser.Content, 42);

        _sessionStore.Save(_currentSession);

        if (_currentSessionVm is not null)
        {
            _currentSessionVm.Title = _currentSession.Title;
            _currentSessionVm.UpdatedAt = _currentSession.UpdatedAt;
        }

        if (isNew)
        {
            RebuildGroups();
            if (_currentSessionVm is not null)
            {
                _suppressSessionLoad = true;
                SelectedNode = _currentSessionVm;
                _suppressSessionLoad = false;
            }
        }
    }

    private static string Truncate(string text, int max)
    {
        text = text.Replace('\n', ' ').Trim();
        return text.Length <= max ? text : text[..max].TrimEnd() + "…";
    }

    private IReadOnlyList<ChatMessage> BuildHistory()
    {
        var list = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(SystemPrompt))
            list.Add(new ChatMessage(ChatRole.System, SystemPrompt.Trim()));

        // Include every message except the empty assistant placeholder at the end.
        foreach (var m in Messages)
        {
            if (m.Role == ChatRole.Assistant && string.IsNullOrEmpty(m.Content))
                continue;
            list.Add(m.ToModel());
        }

        return list;
    }
}
