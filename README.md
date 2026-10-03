# AutoTyper

A modern Windows desktop auto-typing application.

[![Build & Test](https://github.com/amarnath-cdr/Auto-typer/actions/workflows/build.yml/badge.svg)](https://github.com/amarnath-cdr/Auto-typer/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Target Platform](https://img.shields.io/badge/Platform-Windows%20x64-0078D6.svg?logo=windows)](https://microsoft.com/windows)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4.svg?logo=dotnet)](https://dotnet.microsoft.com/)

---

## Overview

**AutoTyper** is an open-source Windows x64 desktop utility designed to manage automated text insertion profiles, shortcuts, and typing configurations. Built using **C#**, **.NET 8**, **WPF**, and the **MVVM (Model-View-ViewModel)** architectural pattern, AutoTyper emphasizes clean separation of concerns, robust JSON persistence, and a polished user interface.

> [!NOTE]
> **Project Status: Phase 1 — Foundation Completed**  
> This release provides profile data modeling, full CRUD profile management, live search filtering, atomic JSON configuration storage, light/dark/system theme switching, and comprehensive unit tests. Background keyboard hooks and keystroke simulation engines are scheduled for Phase 2.

---

## Features

### Currently Implemented (Phase 1)
- **Modern Desktop UI**: Clean card-based design with rounded controls, clear status indicators, and responsive typography (`Segoe UI Variable`).
- **Profile Management**:
  - **Create Profiles**: Add auto-typing profiles with Name, Shortcut trigger, Multiline Text, Comment, Delay, Typing Mode, Capitalization rule, and Repeat Count.
  - **Edit Profiles**: Modify any existing profile configuration via a dedicated editor dialog.
  - **Duplicate Profiles**: One-click profile cloning with automatic naming and UUID generation.
  - **Delete Profiles**: Remove profiles with optional confirmation safety prompts.
  - **Enable / Disable Toggle**: Fast individual profile activation without deletion.
- **Instant Search & Filter**: Real-time filtering across profile names, shortcut keys, comments, and snippet text.
- **Reliable Storage**:
  - Stored in `%APPDATA%\AutoTyper\` (`profiles.json` and `settings.json`).
  - Thread-safe, atomic file writes with `.tmp` swapping to prevent file corruption.
  - Automatic fallback generation for missing or invalid configuration files.
- **Theming System**:
  - Dynamic runtime switching between **Light**, **Dark**, and **System** themes.
  - Automatic Windows 10/11 system dark mode detection via Windows personalization settings.
- **Quality & Verification**:
  - 35 automated xUnit tests validating serialization, storage, search queries, profile validation, and duplication.
  - GitHub Actions CI pipeline executing on every push and pull request.

---

## Screenshots

*(Screenshots will be placed in the [`screenshots/`](screenshots/) directory)*

| Main Window (Profile List & Search) | Add / Edit Profile Dialog |
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

1. **Launch AutoTyper**: Run the application from your start menu or command prompt. Default profiles (`Greeting`, `Email Signature`, `Code Snippet`) are loaded automatically on first launch.
2. **Search Profiles**: Type in the top search bar to filter your library by name, shortcut (e.g. `F7`), or snippet content.
3. **Add a Profile**: Click **+ Add Profile**, specify the shortcut and text to type, adjust the typing delay and capitalization mode, then click **Save Profile**.
4. **Duplicate a Profile**: Select any profile in the list and click **Duplicate** to generate an independent copy.
5. **Adjust Settings**: Click **⚙ Settings** to switch themes (Light / Dark / System), configure the default typing delay, or open the local AppData folder.

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
│   │   ├── AutoTypeProfile.cs      # Core profile model & cloning logic
│   │   ├── AppSettings.cs          # Application preferences model
│   │   ├── TypingMode.cs           # Simulated vs Clipboard enum
│   │   ├── CapitalizationMode.cs   # Text transform mode enum
│   │   └── AppTheme.cs             # System / Light / Dark enum
│   ├── ViewModels/
│   │   ├── ViewModelBase.cs        # INotifyPropertyChanged base implementation
│   │   ├── RelayCommand.cs         # ICommand implementation
│   │   ├── MainViewModel.cs        # Primary dashboard logic & filtering
│   │   ├── ProfileEditorViewModel.cs # Add/Edit modal dialog ViewModel
│   │   └── SettingsViewModel.cs    # Application settings ViewModel
│   ├── Views/
│   │   ├── MainWindow.xaml         # Main dashboard view
│   │   ├── ProfileEditorDialog.xaml# Modal profile editor dialog
│   │   └── SettingsDialog.xaml     # Modal application settings dialog
│   ├── Services/
│   │   ├── IProfileStorageService.cs
│   │   ├── ProfileStorageService.cs# Thread-safe JSON persistence for profiles
│   │   ├── ISettingsStorageService.cs
│   │   ├── SettingsStorageService.cs # Thread-safe JSON persistence for settings
│   │   ├── IThemeService.cs
│   │   └── ThemeService.cs         # Runtime theme resource dictionary manager
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
  - Unit test suite (35 tests)
  - GitHub Actions CI workflow
- [ ] **Phase 2 — Keystroke Simulation & Hook Engine** *(Planned)*
  - Windows low-level global keyboard hooks (`SetWindowsHookEx`)
  - `SendInput` simulation engine
  - Clipboard typing mode with clipboard state restoration
  - Configurable typing delay & human jitter
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