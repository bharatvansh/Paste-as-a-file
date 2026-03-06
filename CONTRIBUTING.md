# Contributing to PasteIt

Thanks for considering a contribution! Here's how to get started.

## Prerequisites

- **Windows x64**
- .NET SDK that can build **.NET Framework 4.8** projects
- Visual Studio 2022 (or Build Tools) with the **.NET Framework 4.8 targeting pack**
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) (only needed to build the installer)

## Getting Started

1. **Fork** this repository on GitHub.
2. **Clone** your fork locally.
3. **Build** the solution:

   ```powershell
   dotnet build .\PasteIt.sln -c Release
   ```

4. **Run the tests** to make sure everything passes:

   ```powershell
   dotnet test .\PasteIt.Core.Tests\PasteIt.Core.Tests.csproj -c Release
   ```

## Making Changes

1. Create a **feature branch** from `main`:

   ```bash
   git checkout -b my-feature
   ```

2. Make your changes. Keep commits focused and descriptive.
3. **Run the tests** before committing — all tests must pass.
4. Open a **Pull Request** against `main` with a clear description of what you changed and why.

## Code Style

- Follow the existing code conventions in the project.
- Use **C# latest** language features where appropriate.
- Keep methods small and focused.
- Add tests for new functionality in `PasteIt.Core.Tests`.

## Reporting Bugs

Please use the [Bug Report](../../issues/new?template=bug_report.md) issue template. Include:
- Steps to reproduce
- Expected vs. actual behavior
- Your Windows version

## Suggesting Features

Use the [Feature Request](../../issues/new?template=feature_request.md) issue template to propose new ideas.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
