# AutoTyper

A modern Windows desktop auto-typing application.

[![Build & Test](https://github.com/amarnath-cdr/Auto-typer/actions/workflows/build.yml/badge.svg)](https://github.com/amarnath-cdr/Auto-typer/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Target Platform](https://img.shields.io/badge/Platform-Windows%20x64-0078D6.svg?logo=windows)](https://microsoft.com/windows)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4.svg?logo=dotnet)](https://dotnet.microsoft.com/)

---

## Overview

**AutoTyper** is an open-source Windows x64 desktop utility designed to manage and automate keystroke simulation, text insertion profiles, and hotkey configurations. Built using **C#**, **.NET 8**, **WPF**, and the **MVVM (Model-View-ViewModel)** architectural pattern, AutoTyper emphasizes clean separation of concerns, robust JSON persistence, layout-independent Unicode typing via `SendInput`, and global hotkey integration.

> [!NOTE]
> **Project Status: Phase 2 — Keystroke Simulation & Global Hotkeys Completed**  
> Character-by-character typing simulation, customizable constant and randomized jitter delays, global hotkeys with `RegisterHotKey`, async cancellation with `CancellationToken`, and modifier safety cleanup are fully implemented and covered by 72 unit tests.

---

## Features

### Currently Implemented (Phase 1 & Phase 2)
- **Character-by-Character Keystroke Simulation**:
  - Layout-independent typing using Windows `SendInput` with `KEYEVENTF_UNICODE`.
  - Non-blocking async execution using `Task.Delay` and `CancellationToken`.
  - Stuck-modifier prevention with automatic `ReleaseAllModifiers()` cleanup on cancel or error.
- **Timing & Random Jitter**:
  - Configurable typing delay (ms).
  - Variable random delay / human jitter within configurable minimum and maximum millisecond bounds.
- **Global Hotkeys**:
  - Global trigger hotkeys per profile (e.g. `F7`, `Ctrl+F8`, `Alt+Shift+T`).
  - Global Stop Hotkey (default: `Escape`) to halt active typing instantly from any window.
  - Conflict detection, validation, and lifecycle unregistration.
- **Profile Management**:
  - **Create / Edit Profiles**: Name, Shortcut, Multiline Text, Comment, Delay, Jitter bounds, Mode, Capitalization, Repeat Count.
  - **Duplicate Profiles**: One-click profile cloning with UUID generation.
  - **Delete Profiles**: Remove profiles with optional confirmation safety prompt.
  - **Enable / Disable Toggle**: Fast individual profile activation without deletion.
- **Instant Search & Filter**: Real-time filtering across profile names, shortcut keys, comments, and snippet text.
- **Reliable Storage**:
  - Stored in `%APPDATA%\AutoTyper\` (`profiles.json` and `settings.json`).
  - Thread-safe, atomic file writes with `.tmp` swapping.
  - Automatic fallback generation for missing or invalid configuration files.
- **Theming System**:
  - Dynamic runtime switching between **Light**, **Dark**, and **System** themes.
  - Automatic Windows system dark mode detection via Windows personalization settings.
- **Quality & Verification**:
  - 72 automated xUnit tests validating typing sequences, delays, jitter, cancellation, hotkeys, serialization, storage, search, and validation.
  - GitHub Actions CI pipeline executing on every push and pull request.

---

## Screenshots

*(Screenshots will be placed in the [`screenshots/`](screenshots/) directory)*

| Main Window (Profile List & Controls) | Add / Edit Profile Dialog |
| :---: | :---: |
| ![Main Window Preview](screenshots/main_window.png) | ![Profile Editor Preview](screenshots/profile_editor.png) |

---

## Requirements

- **Operating System**: Windows 10 (version 1809+) or Windows 11 (x64)
- **Runtime**: [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows x64)

---

## Installation

### From Source
1. Clone the repository:
   ```bash
   git clone https://github.com/amarnath-cdr/Auto-typer.git
   cd Auto-typer
   ```
2. Build and run using the .NET CLI:
   ```powershell
   dotnet run --project AutoTyper/AutoTyper.csproj
   ```

### Standalone Executable
Publish a self-contained or framework-dependent Windows x64 executable:
```powershell
dotnet publish AutoTyper/AutoTyper.csproj -c Release -r win-x64 --self-contained false -o ./publish
```

---

## Usage

1. **Launch AutoTyper**: Default profiles (`Greeting`, `Email Signature`, `Code Snippet`) are loaded automatically on first launch.
2. **Start Typing via Hotkey**: Press the configured global shortcut (e.g. `F7`) to start typing the corresponding profile into any active application (such as Notepad or your code editor).
3. **Start Typing via UI**: Select any profile in the list and click **▶ Start Typing**.
4. **Stop Typing**: Press the global stop hotkey (`Escape`) or click **⏹ Stop** to halt typing immediately.
5. **Configure Jitter**: Open the profile editor, check **Enable Random Jitter**, and set the min/max millisecond bounds.
6. **Adjust Settings**: Click **⚙ Settings** to switch themes, customize the global stop hotkey, or change default profile timing.

---

## Development

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (v8.0.100 or newer)
- Visual Studio 2022 (v17.8+), Visual Studio Code with C# Dev Kit, or JetBrains Rider
- Git for Windows

### Building the Solution
```powershell
# Restore dependencies
dotnet restore AutoTyper.sln

# Build Release binary
dotnet build AutoTyper.sln -c Release --no-restore
```

### Running Unit Tests
```powershell
dotnet test AutoTyper.sln -c Release --verbosity normal
```

---

## Project Structure

```
Auto-typer/
├── .github/
│   └── workflows/
│       └── build.yml               # GitHub Actions CI workflow
├── AutoTyper/
│   ├── AutoTyper.csproj            # WPF Application project file (.NET 8)
│   ├── App.xaml / App.xaml.cs      # Application entry & composition root
│   ├── Models/
│   │   ├── AutoTypeProfile.cs      # Core profile model & jitter properties
│   │   ├── AppSettings.cs          # Application preferences & stop hotkey
│   │   ├── TypingMode.cs           # Simulated vs Clipboard enum
│   │   ├── CapitalizationMode.cs   # Text transform mode enum
│   │   └── AppTheme.cs             # System / Light / Dark enum
│   ├── ViewModels/
│   │   ├── ViewModelBase.cs        # INotifyPropertyChanged base implementation
│   │   ├── RelayCommand.cs         # ICommand implementation
│   │   ├── MainViewModel.cs        # Primary dashboard logic, typing state & hotkey dispatch
│   │   ├── ProfileEditorViewModel.cs # Add/Edit modal dialog ViewModel
│   │   └── SettingsViewModel.cs    # Application settings ViewModel
│   ├── Views/
│   │   ├── MainWindow.xaml         # Main dashboard view with typing controls
│   │   ├── ProfileEditorDialog.xaml# Modal profile editor dialog with jitter inputs
│   │   └── SettingsDialog.xaml     # Modal application settings dialog
│   ├── Services/
│   │   ├── Engine/
│   │   │   ├── ITypingEngine.cs    # Typing engine abstraction
│   │   │   ├── TypingEngine.cs     # Character-by-character async simulation
│   │   │   ├── TypingState.cs      # Engine states (Ready, Typing, Stopped, Error, Completed)
│   │   │   └── IDelayProvider.cs   # Testable delay abstraction
│   │   ├── Keyboard/
│   │   │   ├── IKeyboardSimulator.cs      # Keyboard simulation interface
│   │   │   └── WindowsKeyboardSimulator.cs # Win32 SendInput implementation
│   │   ├── Hotkeys/
│   │   │   ├── IHotkeyService.cs          # Hotkey registration interface
│   │   │   ├── HotkeyModel.cs             # Hotkey parser & representation
│   │   │   └── WindowsHotkeyService.cs    # Win32 RegisterHotKey & HwndSource hook
│   │   ├── IProfileStorageService.cs / ProfileStorageService.cs
│   │   ├── ISettingsStorageService.cs / SettingsStorageService.cs
│   │   └── IThemeService.cs / ThemeService.cs
│   ├── Resources/
│   │   ├── Themes/
│   │   │   ├── LightTheme.xaml     # Light theme palette
│   │   │   └── DarkTheme.xaml      # Dark theme palette
│   │   └── Styles.xaml             # Reusable controls & card styles
│   └── Utilities/
│       ├── PathConstants.cs        # Cross-platform AppData path resolver
│       └── ProfileValidator.cs     # Input validation rules
├── AutoTyper.Tests/
│   ├── AutoTyper.Tests.csproj      # xUnit test project (.NET 8)
│   ├── TypingEngineTests.cs        # Keystroke simulation & timing tests
│   ├── HotkeyServiceTests.cs       # Hotkey parsing & conflict tests
│   ├── MainViewModelPhase2Tests.cs # ViewModel typing integration tests
│   ├── SerializationTests.cs       # JSON roundtrip & formatting tests
│   ├── StorageServiceTests.cs      # File persistence & recovery tests
│   ├── SearchTests.cs              # Search filtering tests
│   ├── ProfileValidationTests.cs   # Validation edge-case tests
│   └── DuplicateProfileTests.cs    # Profile cloning tests
├── screenshots/
│   └── .gitkeep
├── AutoTyper.sln                   # Visual Studio solution file
├── .gitignore                      # .NET & Visual Studio ignore rules
├── LICENSE                         # MIT License
├── README.md                       # Repository documentation
├── CHANGELOG.md                    # Keep a Changelog document
└── CONTRIBUTING.md                 # Contribution guidelines
```

---

## Roadmap

The development of AutoTyper is structured into sequential phases:

- [x] **Phase 1 — Foundation**
  - .NET 8 WPF architecture with MVVM
  - Profile & Settings data models
  - JSON configuration storage in Windows AppData
  - Profile CRUD & Duplication
  - Real-time search filtering
  - Light, Dark, and System theme support
  - Unit test suite
  - GitHub Actions CI workflow
- [x] **Phase 2 — Keystroke Simulation & Global Hotkeys**
  - Win32 `SendInput` simulation engine (`KEYEVENTF_UNICODE`)
  - Character-by-character async execution with `CancellationToken`
  - Modifier safety cleanup on cancellation and errors
  - Configurable typing delay and random jitter bounds
  - Global hotkeys via Win32 `RegisterHotKey` / `HwndSource`
  - Global Stop Hotkey (`Escape`)
  - 72 automated unit tests covering all Phase 1 & 2 behaviors
- [ ] **Phase 3 — Syntax & Key Parser** *(Planned)*
  - Special key token parser (`{ENTER}`, `{TAB}`, `{ESC}`, etc.)
  - Capitalization transform processor
  - Dynamic timestamp & variable insertion
- [ ] **Phase 4 — System Integration & Automation** *(Planned)*
  - Windows system tray minimization & notification icon
  - Global emergency stop hotkey (kill-switch)
  - Profile execution repeat engine with loop limits
  - Launch on Windows startup option
- [ ] **Phase 5 — Distribution & Packaging** *(Planned)*
  - Windows x64 single-file installer & portable release
  - Code signing & GitHub Releases automation

---

## Contributing

Contributions are welcomed! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on code standards, running tests, and submitting pull requests.

---

## License

This project is licensed under the [MIT License](LICENSE) — see the [LICENSE](LICENSE) file for details.