# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.3.0] - 2026-10-06

### Added
- **Special Key Token Parser**:
  - `ITypingParser` abstraction and `TypingParser` engine for template tokenization.
  - Case-insensitive special key tokens: `{ENTER}`, `{RETURN}`, `{TAB}`, `{BACKSPACE}`, `{BACK}`, `{SPACE}`, `{ESC}`, `{ESCAPE}`, `{UP}`, `{DOWN}`, `{LEFT}`, `{RIGHT}`, `{HOME}`, `{END}`, `{DELETE}`, `{DEL}`, `{INSERT}`, `{INS}`, `{PAGEUP}`, `{PGUP}`, `{PAGEDOWN}`, `{PGDN}`, `{CAPSLOCK}`, `{NUMLOCK}`, `{SCROLLLOCK}`, `{PRINTSCREEN}`, `{PRTSC}`, `{PAUSE}`, `{BREAK}`.
  - Function key tokens: `{F1}` through `{F12}`.
  - Modifier combination tokens: `{CTRL+C}`, `{CTRL+V}`, `{CTRL+A}`, `{CTRL+Z}`, `{CTRL+S}`, `{SHIFT+TAB}`, `{ALT+TAB}`, `{WIN+D}`, `{CTRL+SHIFT+S}`, `{CTRL+ALT+DELETE}`.
  - Literal brace escaping using `{{` for `{` and `}}` for `}` (e.g. `{{CTRL+C}}` outputs `{CTRL+C}`).
  - Graceful fallback for non-matching braces (e.g. `{world}` treated as literal text).
- **Advanced Typing & Execution Modes**:
  - **Clipboard Typing Mode**: `IClipboardService` and `WindowsClipboardService` for instantaneous paste via `Ctrl+V` with automatic preservation and restoration of previous clipboard contents.
  - **Start Delay**: Configurable `StartDelayMs` (0 to 60,000ms) with full cancellation support.
  - **Repeat Count**: Repeat loop execution (`RepeatCount`, 1 to 1,000) with cancellation checks.
  - **Capitalization Transforms**: `Uppercase`, `Lowercase`, and `SentenceCase` text transformation modes.
- **Keyboard Simulator Extensions**:
  - `SendKeyDown`, `SendKeyUp`, and `SendKeyCombination` added to `IKeyboardSimulator` and `WindowsKeyboardSimulator`.
  - Guaranteed modifier cleanup on cancellation or error to prevent stuck keys.
- **UI Enhancements**:
  - `ProfileEditorDialog`: Added `Start Delay (ms)` input, syntax reference card for special keys & combinations, and updated layout.
- **Testing**:
  - 75 new unit tests (147 total) covering parser edge cases, special keys, combinations, brace escaping, start delays, repeat loops, clipboard mode, capitalization modes, and validation.

## [0.2.0] - 2026-10-05

### Added
- **Keystroke Simulation Engine**:
  - `IKeyboardSimulator` abstraction and `WindowsKeyboardSimulator` using Win32 `SendInput` with `KEYEVENTF_UNICODE` for layout-independent typing.
  - `ITypingEngine` and `TypingEngine` for async character-by-character text simulation.
  - State tracking: `Ready`, `Typing`, `Stopped`, `Completed`, `Error`.
  - Progress reporting per character: `CurrentIndex`, `TotalCharacters`, and completion percentage.
  - Modifier safety cleanup: `ReleaseAllModifiers()` automatically releases Shift, Ctrl, Alt, and Win keys on cancellation or exception.
  - `IDelayProvider` and `TaskDelayProvider` abstractions for deterministic testing of typing delays.
- **Timing & Random Jitter**:
  - Configurable constant typing delay (ms).
  - Configurable random jitter range (`MinDelayMs` to `MaxDelayMs`) for human-like typing simulation.
- **Global Hotkey Support**:
  - `IHotkeyService` abstraction and `WindowsHotkeyService` utilizing Win32 `RegisterHotKey`/`UnregisterHotKey` with WPF `HwndSource` message hook.
  - `HotkeyModel` parser supporting function keys, modifiers (Ctrl, Alt, Shift, Win), and special keys (Escape, Tab, Space, Enter).
  - Global Stop Hotkey (default: `Escape`) to cancel typing from any foreground window.
  - Hotkey conflict detection, validation, and lifecycle unregistration.
- **UI Enhancements**:
  - Start Typing (`▶`) and Stop (`⏹`) action buttons in MainWindow.
  - Real-time typing status and character progress display.
  - Jitter enable checkbox and min/max delay numeric inputs in `ProfileEditorDialog`.
  - Global stop hotkey configuration in `SettingsDialog`.
- **Testing**:
  - 37 new unit tests (72 total) covering keystroke sequencing, delay timing, jitter bounds, cancellation mid-typing, modifier cleanup, hotkey parsing/conflicts, and ViewModel lifecycle.

## [0.1.0] - 2026-10-03

### Added
- **Foundation Architecture**: .NET 8 WPF desktop application with clean MVVM pattern.
- **Data Models**:
  - `AutoTypeProfile` with support for shortcut key, name, comment, text content, typing mode (Simulated / Clipboard), delay, capitalization mode, repeat count, and enabled state.
  - `AppSettings` with theme preference, master switch state, deletion confirmation, and default typing delay.
- **Persistence Layer**:
  - JSON configuration storage in Windows AppData (`%APPDATA%\AutoTyper\`).
  - Safe file writes with error recovery and fallback defaults.
- **User Interface**:
  - Modern Windows desktop UI with rounded corners, subtle elevations, and clean typography.
  - Profile search filtering across name, comment, shortcut, and text.
  - Profile cards and table view displaying shortcut badge, status, delay, and preview.
  - Profile Add/Edit modal dialog with full property editing.
  - Profile duplication and deletion operations.
  - Settings dialog with theme switching and configuration location.
  - Light, Dark, and System theme resource dictionaries.
- **Testing**:
  - Unit test suite covering JSON serialization/deserialization, search filtering, profile validation, storage service operations, and duplication logic.
- **CI/CD & Documentation**:
  - GitHub Actions CI workflow for automated building and unit testing on Windows.
  - Project documentation including README, CONTRIBUTING, LICENSE, and CHANGELOG.
