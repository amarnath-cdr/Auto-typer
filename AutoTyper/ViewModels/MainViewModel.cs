using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoTyper.Models;
using AutoTyper.Services;
using AutoTyper.Services.Engine;
using AutoTyper.Services.Hotkeys;

namespace AutoTyper.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IProfileStorageService _profileStorage;
    private readonly ISettingsStorageService _settingsStorage;
    private readonly IThemeService _themeService;
    private readonly ITypingEngine _typingEngine;
    private readonly IHotkeyService _hotkeyService;

    private readonly List<AutoTypeProfile> _allProfiles = new();
    private readonly Dictionary<int, AutoTypeProfile> _hotkeyProfileMap = new();
    private const int StopHotkeyId = 99999;
    private int _nextHotkeyId = 1000;

    private AutoTypeProfile? _selectedProfile;
    private string _searchText = string.Empty;
    private string _statusMessage = "Ready";
    private bool _isMasterEnabled = true;
    private TypingState _currentTypingState = TypingState.Ready;
    private string _typingProgressText = string.Empty;
    private CancellationTokenSource? _typingCts;
    private bool _disposed;

    public MainViewModel(
        IProfileStorageService profileStorage,
        ISettingsStorageService settingsStorage,
        IThemeService themeService,
        ITypingEngine typingEngine,
        IHotkeyService hotkeyService)
    {
        _profileStorage = profileStorage ?? throw new ArgumentNullException(nameof(profileStorage));
        _settingsStorage = settingsStorage ?? throw new ArgumentNullException(nameof(settingsStorage));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _typingEngine = typingEngine ?? throw new ArgumentNullException(nameof(typingEngine));
        _hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));

        Profiles = new ObservableCollection<AutoTypeProfile>();

        AddCommand = new RelayCommand(AddProfile, () => !IsTyping);
        EditCommand = new RelayCommand(EditProfile, () => SelectedProfile != null && !IsTyping);
        DuplicateCommand = new RelayCommand(DuplicateProfile, () => SelectedProfile != null && !IsTyping);
        DeleteCommand = new RelayCommand(DeleteProfile, () => SelectedProfile != null && !IsTyping);
        ToggleEnableCommand = new RelayCommand<AutoTypeProfile>(ToggleProfile, _ => !IsTyping);
        ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);
        OpenSettingsCommand = new RelayCommand(OpenSettings, () => !IsTyping);

        StartTypingCommand = new RelayCommand(StartTypingSelected, () => SelectedProfile != null && !IsTyping && IsMasterEnabled);
        StopTypingCommand = new RelayCommand(StopTyping, () => IsTyping);

        _typingEngine.StateChanged += OnTypingEngineStateChanged;
        _typingEngine.ProgressChanged += OnTypingEngineProgressChanged;
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;

        LoadData();
    }

    public ObservableCollection<AutoTypeProfile> Profiles { get; }

    public AutoTypeProfile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetProperty(ref _selectedProfile, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsMasterEnabled
    {
        get => _isMasterEnabled;
        set
        {
            if (SetProperty(ref _isMasterEnabled, value))
            {
                var settings = _settingsStorage.LoadSettings();
                settings.AutoTyperMasterEnabled = value;
                _settingsStorage.SaveSettings(settings);
                OnPropertyChanged(nameof(MasterEnabledText));
                RegisterGlobalHotkeys();
            }
        }
    }

    public string MasterEnabledText => IsMasterEnabled ? "Enabled" : "Disabled";

    public TypingState CurrentTypingState
    {
        get => _currentTypingState;
        private set
        {
            if (SetProperty(ref _currentTypingState, value))
            {
                OnPropertyChanged(nameof(IsTyping));
                OnPropertyChanged(nameof(StateDisplayText));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsTyping => CurrentTypingState == TypingState.Typing;

    public string StateDisplayText => CurrentTypingState switch
    {
        TypingState.Ready => "Ready",
        TypingState.Typing => "Typing...",
        TypingState.Stopped => "Stopped",
        TypingState.Completed => "Completed",
        TypingState.Error => "Error",
        _ => "Ready"
    };

    public string TypingProgressText
    {
        get => _typingProgressText;
        private set => SetProperty(ref _typingProgressText, value);
    }

    public int TotalProfilesCount => _allProfiles.Count;
    public int EnabledProfilesCount => _allProfiles.Count(p => p.IsEnabled);

    // Callbacks for View to show Dialogs
    public Func<AutoTypeProfile?, (bool Success, AutoTypeProfile? Profile)>? ShowProfileEditorDialog { get; set; }
    public Action? ShowSettingsDialog { get; set; }
    public Func<string, string, bool>? ConfirmAction { get; set; }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DuplicateCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleEnableCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand StartTypingCommand { get; }
    public ICommand StopTypingCommand { get; }

    private void LoadData()
    {
        var settings = _settingsStorage.LoadSettings();
        _isMasterEnabled = settings.AutoTyperMasterEnabled;
        _themeService.ApplyTheme(settings.Theme);

        var loaded = _profileStorage.LoadProfiles();
        _allProfiles.Clear();
        _allProfiles.AddRange(loaded);

        ApplyFilter();
        UpdateCounts();
        RegisterGlobalHotkeys();
    }

    public void RegisterGlobalHotkeys()
    {
        _hotkeyService.UnregisterAll();
        _hotkeyProfileMap.Clear();

        if (!IsMasterEnabled)
            return;

        var settings = _settingsStorage.LoadSettings();

        // Register global stop hotkey
        if (!string.IsNullOrWhiteSpace(settings.GlobalStopHotkey) &&
            HotkeyModel.TryParse(settings.GlobalStopHotkey, out var stopHotkey) && stopHotkey != null)
        {
            _hotkeyService.Register(StopHotkeyId, stopHotkey);
        }

        // Register each enabled profile shortcut
        _nextHotkeyId = 1000;
        foreach (var profile in _allProfiles.Where(p => p.IsEnabled && !string.IsNullOrWhiteSpace(p.Shortcut)))
        {
            if (HotkeyModel.TryParse(profile.Shortcut, out var hotkey) && hotkey != null)
            {
                if (!_hotkeyService.HasConflict(hotkey))
                {
                    int id = _nextHotkeyId++;
                    if (_hotkeyService.Register(id, hotkey))
                    {
                        _hotkeyProfileMap[id] = profile;
                    }
                }
            }
        }
    }

    public void ApplyFilter()
    {
        var query = SearchText?.Trim();

        IEnumerable<AutoTypeProfile> filtered = _allProfiles;
        if (!string.IsNullOrEmpty(query))
        {
            filtered = _allProfiles.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Shortcut.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (p.Comment != null && p.Comment.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (p.Text != null && p.Text.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        Profiles.Clear();
        foreach (var profile in filtered)
        {
            Profiles.Add(profile);
        }

        if (SelectedProfile != null && !Profiles.Contains(SelectedProfile))
        {
            SelectedProfile = Profiles.FirstOrDefault();
        }
    }

    public async Task StartTypingAsync(AutoTypeProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (IsTyping)
        {
            StopTyping();
            return;
        }

        _typingCts = new CancellationTokenSource();
        StatusMessage = $"Typing profile '{profile.Name}'...";

        try
        {
            await _typingEngine.TypeTextAsync(
                profile.Text,
                profile.TypingDelayMs,
                profile.UseJitter,
                profile.MinDelayMs,
                profile.MaxDelayMs,
                _typingCts.Token);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = $"Typing of '{profile.Name}' was stopped.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error typing '{profile.Name}': {ex.Message}";
        }
        finally
        {
            _typingCts.Dispose();
            _typingCts = null;
        }
    }

    private void StartTypingSelected()
    {
        if (SelectedProfile != null)
        {
            _ = StartTypingAsync(SelectedProfile);
        }
    }

    public void StopTyping()
    {
        if (_typingCts != null && !_typingCts.IsCancellationRequested)
        {
            _typingCts.Cancel();
            StatusMessage = "Stopping typing...";
        }
    }

    private void OnHotkeyPressed(object? sender, int hotkeyId)
    {
        if (hotkeyId == StopHotkeyId)
        {
            StopTyping();
            return;
        }

        if (_hotkeyProfileMap.TryGetValue(hotkeyId, out var profile))
        {
            if (IsTyping)
            {
                StopTyping();
            }
            else if (IsMasterEnabled && profile.IsEnabled)
            {
                SelectedProfile = profile;
                _ = StartTypingAsync(profile);
            }
        }
    }

    private void OnTypingEngineStateChanged(object? sender, TypingState state)
    {
        CurrentTypingState = state;
        if (state == TypingState.Completed)
        {
            StatusMessage = "Typing completed.";
            TypingProgressText = string.Empty;
        }
        else if (state == TypingState.Stopped)
        {
            StatusMessage = "Typing stopped.";
            TypingProgressText = string.Empty;
        }
    }

    private void OnTypingEngineProgressChanged(object? sender, TypingProgress progress)
    {
        TypingProgressText = $"Typing: {progress.CurrentIndex}/{progress.TotalCharacters} chars ({progress.PercentComplete:F0}%)";
    }

    private void AddProfile()
    {
        if (ShowProfileEditorDialog == null)
            return;

        var (success, newProfile) = ShowProfileEditorDialog(null);
        if (success && newProfile != null)
        {
            _allProfiles.Add(newProfile);
            SaveProfiles();
            ApplyFilter();
            SelectedProfile = newProfile;
            StatusMessage = $"Added profile '{newProfile.Name}'.";
            UpdateCounts();
            RegisterGlobalHotkeys();
        }
    }

    private void EditProfile()
    {
        if (SelectedProfile == null || ShowProfileEditorDialog == null)
            return;

        var profileToEdit = SelectedProfile;
        var (success, updatedProfile) = ShowProfileEditorDialog(profileToEdit);
        if (success && updatedProfile != null)
        {
            profileToEdit.CopyFrom(updatedProfile);
            SaveProfiles();
            ApplyFilter();
            SelectedProfile = profileToEdit;
            StatusMessage = $"Updated profile '{profileToEdit.Name}'.";
            UpdateCounts();
            RegisterGlobalHotkeys();
        }
    }

    private void DuplicateProfile()
    {
        if (SelectedProfile == null)
            return;

        var clone = SelectedProfile.Clone();
        _allProfiles.Add(clone);
        SaveProfiles();
        ApplyFilter();
        SelectedProfile = clone;
        StatusMessage = $"Duplicated profile as '{clone.Name}'.";
        UpdateCounts();
        RegisterGlobalHotkeys();
    }

    private void DeleteProfile()
    {
        if (SelectedProfile == null)
            return;

        var settings = _settingsStorage.LoadSettings();
        if (settings.ConfirmOnDelete && ConfirmAction != null)
        {
            var confirmed = ConfirmAction(
                $"Are you sure you want to delete profile '{SelectedProfile.Name}'?",
                "Confirm Deletion");
            if (!confirmed)
                return;
        }

        var deletedName = SelectedProfile.Name;
        _allProfiles.Remove(SelectedProfile);
        SaveProfiles();
        ApplyFilter();
        SelectedProfile = Profiles.FirstOrDefault();
        StatusMessage = $"Deleted profile '{deletedName}'.";
        UpdateCounts();
        RegisterGlobalHotkeys();
    }

    private void ToggleProfile(AutoTypeProfile? profile)
    {
        if (profile == null)
            return;

        profile.IsEnabled = !profile.IsEnabled;
        profile.ModifiedAt = DateTimeOffset.UtcNow;
        SaveProfiles();
        ApplyFilter();
        StatusMessage = $"Profile '{profile.Name}' {(profile.IsEnabled ? "enabled" : "disabled")}.";
        UpdateCounts();
        RegisterGlobalHotkeys();
    }

    private void OpenSettings()
    {
        ShowSettingsDialog?.Invoke();
        var settings = _settingsStorage.LoadSettings();
        IsMasterEnabled = settings.AutoTyperMasterEnabled;
        RegisterGlobalHotkeys();
    }

    private void SaveProfiles()
    {
        _profileStorage.SaveProfiles(_allProfiles);
    }

    private void UpdateCounts()
    {
        OnPropertyChanged(nameof(TotalProfilesCount));
        OnPropertyChanged(nameof(EnabledProfilesCount));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _typingCts?.Cancel();
            _typingCts?.Dispose();
            _typingEngine.StateChanged -= OnTypingEngineStateChanged;
            _typingEngine.ProgressChanged -= OnTypingEngineProgressChanged;
            _hotkeyService.HotkeyPressed -= OnHotkeyPressed;
            _hotkeyService.Dispose();
            _disposed = true;
        }
    }
}
