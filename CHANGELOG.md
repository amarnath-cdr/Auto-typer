# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
