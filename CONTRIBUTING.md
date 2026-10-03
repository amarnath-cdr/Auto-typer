# Contributing to AutoTyper

Thank you for your interest in contributing to AutoTyper! We welcome bug reports, feature suggestions, documentation improvements, and pull requests.

## Code of Conduct

Please treat everyone with respect, kindness, and professional courtesy.

## How to Contribute

### Reporting Bugs
- Search existing issues to ensure the bug hasn't already been reported.
- Open a new issue with a clear title and description.
- Include your Windows version, .NET version, and steps to reproduce the issue.

### Suggesting Enhancements
- Open an issue describing the feature, why it is useful, and potential implementation approaches.

### Pull Requests
1. Fork the repository.
2. Create a feature branch: `git checkout -b feature/my-new-feature`.
3. Follow the project's coding standards:
   - C# 12 / .NET 8 idioms.
   - MVVM architecture separation (Views have no business logic).
   - Write unit tests for new logic in `AutoTyper.Tests`.
4. Ensure all tests pass: `dotnet test`.
5. Commit your changes with descriptive commit messages following Conventional Commits (e.g., `feat: ...`, `fix: ...`, `docs: ...`).
6. Push to your branch and submit a Pull Request against `main`.

## Development Setup

- Windows 10/11 x64
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (with ".NET Desktop Development" workload) or Visual Studio Code with C# Dev Kit / JetBrains Rider

### Build & Test via CLI
```powershell
# Restore dependencies
dotnet restore

# Build solution
dotnet build -c Release

# Run tests
dotnet test -c Release
```
