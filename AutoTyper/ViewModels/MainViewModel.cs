using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using AutoTyper.Models;
using AutoTyper.Services;

namespace AutoTyper.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IProfileStorageService _profileStorage;
    private readonly ISettingsStorageService _settingsStorage;
    private readonly IThemeService _themeService;

    private readonly List<AutoTypeProfile> _allProfiles = new();
    private AutoTypeProfile? _selectedProfile;
    private string _searchText = string.Empty;
    private string _statusMessage = "Ready";
    private bool _isMasterEnabled = true;

    public MainViewModel(
        IProfileStorageService profileStorage,
        ISettingsStorageService settingsStorage,
        IThemeService themeService)
    {
        _profileStorage = profileStorage ?? throw new ArgumentNullException(nameof(profileStorage));
        _settingsStorage = settingsStorage ?? throw new ArgumentNullException(nameof(settingsStorage));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

        Profiles = new ObservableCollection<AutoTypeProfile>();

        AddCommand = new RelayCommand(AddProfile);
        EditCommand = new RelayCommand(EditProfile, () => SelectedProfile != null);
        DuplicateCommand = new RelayCommand(DuplicateProfile, () => SelectedProfile != null);
        DeleteCommand = new RelayCommand(DeleteProfile, () => SelectedProfile != null);
        ToggleEnableCommand = new RelayCommand<AutoTypeProfile>(ToggleProfile);
        ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);
        OpenSettingsCommand = new RelayCommand(OpenSettings);

        LoadData();
    }

    public ObservableCollection<AutoTypeProfile> Profiles { get; }

    public AutoTypeProfile? SelectedProfile
    {
        get => _selectedProfile;
        set => SetProperty(ref _selectedProfile, value);
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
            }
        }
    }

    public string MasterEnabledText => IsMasterEnabled ? "Enabled" : "Disabled";

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
    }

    private void OpenSettings()
    {
        ShowSettingsDialog?.Invoke();
        var settings = _settingsStorage.LoadSettings();
        IsMasterEnabled = settings.AutoTyperMasterEnabled;
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
}
