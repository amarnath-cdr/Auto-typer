using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using AutoTyper.Models;
using AutoTyper.Utilities;

namespace AutoTyper.ViewModels;

public class ProfileEditorViewModel : ViewModelBase
{
    private string _name = string.Empty;
    private string _shortcut = "F7";
    private string _text = string.Empty;
    private string _comment = string.Empty;
    private int _startDelayMs = 0;
    private int _typingDelayMs = 20;
    private bool _useJitter;
    private int _minDelayMs = 10;
    private int _maxDelayMs = 50;
    private TypingMode _typingMode = TypingMode.Simulated;
    private CapitalizationMode _capitalization = CapitalizationMode.Original;
    private int _repeatCount = 1;
    private bool _isEnabled = true;
    private string? _errorMessage;

    public ProfileEditorViewModel(AutoTypeProfile? existingProfile = null)
    {
        IsNew = existingProfile == null;
        OriginalId = existingProfile?.Id ?? Guid.NewGuid();

        if (existingProfile != null)
        {
            _name = existingProfile.Name;
            _shortcut = existingProfile.Shortcut;
            _text = existingProfile.Text;
            _comment = existingProfile.Comment;
            _startDelayMs = existingProfile.StartDelayMs;
            _typingDelayMs = existingProfile.TypingDelayMs;
            _useJitter = existingProfile.UseJitter;
            _minDelayMs = existingProfile.MinDelayMs;
            _maxDelayMs = existingProfile.MaxDelayMs;
            _typingMode = existingProfile.TypingMode;
            _capitalization = existingProfile.Capitalization;
            _repeatCount = existingProfile.RepeatCount;
            _isEnabled = existingProfile.IsEnabled;
        }

        SaveCommand = new RelayCommand(Save, CanSave);
        CancelCommand = new RelayCommand(Cancel);

        PopulateShortcuts();
    }

    public Guid OriginalId { get; }
    public bool IsNew { get; }
    public bool DialogResult { get; private set; }

    public event Action? RequestClose;

    public string Title => IsNew ? "Add New Profile" : "Edit Profile";

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                Validate();
            }
        }
    }

    public string Shortcut
    {
        get => _shortcut;
        set
        {
            if (SetProperty(ref _shortcut, value))
            {
                Validate();
            }
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value))
            {
                Validate();
            }
        }
    }

    public string Comment
    {
        get => _comment;
        set => SetProperty(ref _comment, value);
    }

    public int StartDelayMs
    {
        get => _startDelayMs;
        set
        {
            if (SetProperty(ref _startDelayMs, value))
            {
                Validate();
            }
        }
    }

    public int TypingDelayMs
    {
        get => _typingDelayMs;
        set
        {
            if (SetProperty(ref _typingDelayMs, value))
            {
                Validate();
            }
        }
    }

    public bool UseJitter
    {
        get => _useJitter;
        set
        {
            if (SetProperty(ref _useJitter, value))
            {
                Validate();
            }
        }
    }

    public int MinDelayMs
    {
        get => _minDelayMs;
        set
        {
            if (SetProperty(ref _minDelayMs, value))
            {
                Validate();
            }
        }
    }

    public int MaxDelayMs
    {
        get => _maxDelayMs;
        set
        {
            if (SetProperty(ref _maxDelayMs, value))
            {
                Validate();
            }
        }
    }

    public TypingMode TypingMode
    {
        get => _typingMode;
        set => SetProperty(ref _typingMode, value);
    }

    public CapitalizationMode Capitalization
    {
        get => _capitalization;
        set => SetProperty(ref _capitalization, value);
    }

    public int RepeatCount
    {
        get => _repeatCount;
        set
        {
            if (SetProperty(ref _repeatCount, value))
            {
                Validate();
            }
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ObservableCollection<string> AvailableShortcuts { get; } = new();

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private void PopulateShortcuts()
    {
        for (int i = 1; i <= 12; i++)
        {
            AvailableShortcuts.Add($"F{i}");
        }
        for (int i = 1; i <= 12; i++)
        {
            AvailableShortcuts.Add($"Ctrl+F{i}");
        }
        for (int i = 1; i <= 12; i++)
        {
            AvailableShortcuts.Add($"Alt+F{i}");
        }
        for (int i = 1; i <= 12; i++)
        {
            AvailableShortcuts.Add($"Shift+F{i}");
        }

        if (!AvailableShortcuts.Contains(Shortcut) && !string.IsNullOrWhiteSpace(Shortcut))
        {
            AvailableShortcuts.Insert(0, Shortcut);
        }
    }

    public AutoTypeProfile BuildProfile()
    {
        return new AutoTypeProfile
        {
            Id = OriginalId,
            Name = Name.Trim(),
            Shortcut = Shortcut.Trim(),
            Text = Text ?? string.Empty,
            Comment = Comment?.Trim() ?? string.Empty,
            StartDelayMs = StartDelayMs,
            TypingDelayMs = TypingDelayMs,
            UseJitter = UseJitter,
            MinDelayMs = MinDelayMs,
            MaxDelayMs = MaxDelayMs,
            TypingMode = TypingMode,
            Capitalization = Capitalization,
            RepeatCount = Math.Max(1, RepeatCount),
            IsEnabled = IsEnabled,
            ModifiedAt = DateTimeOffset.UtcNow
        };
    }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(Shortcut) &&
               StartDelayMs >= 0 &&
               TypingDelayMs >= 0 &&
               RepeatCount >= 1;
    }

    private void Validate()
    {
        var temp = BuildProfile();
        var result = ProfileValidator.Validate(temp);
        ErrorMessage = result.IsValid ? null : string.Join(" ", result.Errors);
    }

    private void Save()
    {
        Validate();
        if (ErrorMessage != null)
            return;

        DialogResult = true;
        RequestClose?.Invoke();
    }

    private void Cancel()
    {
        DialogResult = false;
        RequestClose?.Invoke();
    }
}
