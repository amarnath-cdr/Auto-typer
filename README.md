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
> **Project Status: Phase 5 — Distribution & Packaging Completed**  
> AutoTyper is now a polished, 1.0 release-ready desktop utility. It features robust keystroke simulation, robust system tray integration, single-instance protection, and a highly customizable typing engine.

---

## Features
- **System Tray & Desktop Integration**:
  - Windows notification area tray icon with context menu (`Show AutoTyper`, `Start Selected Profile`, `Stop Typing`, `Exit`).
  - **Minimize to Tray**: Keeps AutoTyper running unobtrusively in the tray when minimized.
  - **Close to Tray**: Keeps AutoTyper active when clicking the window close button (`X`).
  - **Start with Windows**: Per-user `Run` registry integration for auto-launch on login without requiring administrator privileges.
  - **Windows Notifications**: Balloon tips for typing start, completion, cancellation, and error states.
  - **Single-Instance Protection**: Named Mutex and EventWaitHandle prevent duplicate instances and bring the active window to the foreground on secondary launches.
- **Special Key & Combination Parser**:
  - Special keys: `{ENTER}`, `{TAB}`, `{BACKSPACE}`, `{ESC}`, `{UP}`, `{DOWN}`, `{LEFT}`, `{RIGHT}`, `{HOME}`, `{END}`, `{DELETE}`, `{INSERT}`, `{PAGEUP}`, `{PAGEDOWN}`, `{CAPSLOCK}`, `{NUMLOCK}`, `{SCROLLLOCK}`, `{PRINTSCREEN}`, `{PAUSE}`.
  - Function keys: `{F1}` through `{F12}`.
  - Modifier combinations: `{CTRL+C}`, `{CTRL+V}`, `{CTRL+A}`, `{CTRL+Z}`, `{CTRL+S}`, `{SHIFT+TAB}`, `{ALT+TAB}`, `{WIN+D}`, `{CTRL+SHIFT+S}`, etc.
  - Literal brace escaping: `{{` for `{`, `}}` for `}` (e.g. `{{CTRL+C}}` outputs `{CTRL+C}`).
  - Explicit waits: `{WAIT:ms}` pauses typing for the specified milliseconds (e.g. `{WAIT:1000}` pauses for 1 second). *Note: Supported in Simulated Mode only; Clipboard Mode treats this as literal text.*
  - Graceful fallback: Non-token braces (e.g. `{world}`) are automatically preserved as literal text.
- **Advanced Execution Modes & Options**:
  - **Simulated Mode**: Token-by-token and character-by-character typing via `SendInput`.
  - **Clipboard Mode**: Fast clipboard paste via `Ctrl+V` with automatic clipboard backup and restoration.
  - **Start Delay**: Configurable initial wait (`StartDelayMs`, 0–60,000ms) with cancellation support.
  - **Repeat Count**: Repeat loop execution (`RepeatCount`, 1–1,000) with cancellation checks.
  - **Capitalization Transforms**: `Original`, `Uppercase`, `Lowercase`, and `SentenceCase`.
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
  - **Create / Edit Profiles**: Name, Shortcut, Multiline Text, Comment, Start Delay, Delay, Jitter bounds, Mode, Capitalization, Repeat Count.
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
  - 163 automated xUnit tests validating tray integration, single instance mutex, startup registry, notifications, parsing, combinations, delays, jitter, cancellation, clipboard mode, hotkeys, serialization, storage, search, and validation.
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
Publish a framework-dependent Windows x64 executable (requires .NET 8 Desktop Runtime):
```powershell
dotnet publish AutoTyper/AutoTyper.csproj -c Release -r win-x64 --self-contained false -o ./artifacts/release
```
The resulting executable will be available at `artifacts/release/AutoTyper.exe`.

---

## Usage

1. **Launch AutoTyper**: Default profiles (`Greeting`, `Email Signature`, `Code Snippet`) are loaded automatically on first launch.
2. **Start Typing via Hotkey**: Press the configured global shortcut (e.g. `F7`) to start typing the corresponding profile into any active application (such as Notepad or your code editor).
3. **Start Typing via UI**: Select any profile in the list and click **▶ Start Typing**.
4. **Stop Typing**: Press the global stop hotkey (`Escape`) or click **⏹ Stop** to halt typing immediately.
5. **Configure Jitter**: Open the profile editor, check **Enable Random Jitter**, and set the min/max millisecond bounds.
6. **Adjust Settings**: Click **⚙ Settings** to switch themes, customize the global stop hotkey, or change default profile timing.

---

## Troubleshooting

- **Global Hotkeys Not Working**: Ensure no other application (like a screen recorder or game overlay) is using the same shortcut. The global stop hotkey (`Escape` by default) overrides all profiles.
- **Typing Is Missing Characters or Out of Order**: In Simulated Mode, applications might not process keystrokes fast enough. Edit your profile, check **Enable Random Jitter**, and slightly increase the typing delays (e.g. `20ms` to `50ms`).
- **Wait Tokens Ignored**: The `{WAIT:ms}` token is supported in Simulated Mode only. In Clipboard mode, the entire text payload is pasted instantly via `Ctrl+V`, making sequential wait tokens semantically invalid. They will be treated as literal text.
- **AutoTyper Doesn't Start**: Check the system tray (notification area). AutoTyper uses single-instance mutex protection and defaults to minimizing to the tray. 

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
│   │   ├── Tray/
│   │   │   ├── ITrayIconService.cs        # Tray icon abstraction
│   │   │   └── WindowsTrayIconService.cs  # Windows NotifyIcon implementation
│   │   ├── Startup/
│   │   │   ├── IStartupService.cs         # Windows startup interface
│   │   │   └── WindowsStartupService.cs   # HKCU Run registry implementation
│   │   ├── Notifications/
│   │   │   ├── INotificationService.cs    # Notification service interface
│   │   │   └── WindowsNotificationService.cs # Tray balloon tip dispatcher
│   │   ├── Lifecycle/
│   │   │   ├── ISingleInstanceService.cs  # Single-instance abstraction
│   │   │   └── WindowsSingleInstanceService.cs # Named Mutex & EventWaitHandle
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
│   ├── TypingParserTests.cs        # Special key & modifier token parser tests
│   ├── HotkeyServiceTests.cs       # Hotkey parsing & conflict tests
│   ├── MainViewModelPhase2Tests.cs # ViewModel typing integration tests
│   ├── MainViewModelPhase3Tests.cs # Advanced typing & parser integration tests
│   ├── MainViewModelPhase4Tests.cs # Desktop & tray integration tests
│   ├── TrayAndLifecycleTests.cs    # Tray state & single instance tests
│   ├── StartupServiceTests.cs      # Windows startup registry tests
│   ├── NotificationServiceTests.cs # Balloon notification tests
│   ├── AppSettingsTests.cs         # App settings & fallback recovery tests
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
- [x] **Phase 3 — Syntax & Key Parser**
  - Special key token parser (`{ENTER}`, `{TAB}`, `{ESC}`, navigation & function keys)
  - Modifier combinations (`{CTRL+C}`, `{CTRL+V}`, `{CTRL+SHIFT+S}`, etc.)
  - Literal brace escaping (`{{` / `}}`)
  - Clipboard typing mode with automatic clipboard restore
  - Start Delay (0–60s) & Repeat loop execution (1–1,000)
  - Capitalization transform processor (`Original`, `Uppercase`, `Lowercase`, `SentenceCase`)
  - 147 automated unit tests
- [x] **Phase 4 — System Integration & Automation**
  - Windows system tray minimization & notification icon
  - Tray context menu with `Show AutoTyper`, `Start Selected Profile`, `Stop Typing`, `Exit`
  - Minimize to Tray & Close to Tray window lifecycles
  - Launch on Windows startup option (per-user registry `Run` key)
  - Windows event balloon notifications for typing transitions
  - Single-instance application protection via named Mutex & activation handle
  - 163 automated unit tests
- [x] **Phase 5 — Distribution & Packaging**
  - Project documentation & portfolio presentation
  - Publish command & runtime integration testing
  - Windows UI layout polish and version validation
  - Final test suite validation (176 automated tests)

---

## Contributing

Contributions are welcomed! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on code standards, running tests, and submitting pull requests.

---

## License

This project is licensed under the [MIT License](LICENSE) — see the [LICENSE](LICENSE) file for details.