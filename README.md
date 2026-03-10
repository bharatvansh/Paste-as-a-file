# PasteIt

**PasteIt** is a Windows utility that lets you save clipboard content directly as a file from Windows Explorer. You can trigger it from the Explorer right-click menu or with the global `Ctrl+Shift+V` hotkey when Explorer is focused.

PasteIt currently targets **Windows x64** and is built on **.NET Framework 4.8**.

## What It Can Paste

PasteIt detects the clipboard content type and offers matching file extensions in the Explorer context menu:

- **Plain text**: Saved as `.txt`, with `.md` also offered for markdown-like content.
- **Code snippets / structured text**: Detects many formats including Python, JavaScript, TypeScript, C#, Java, C/C++, HTML, CSS, XML, SQL, Go, Rust, Kotlin, Swift, PHP, Ruby, Shell, PowerShell, Dart, JSON, Markdown, TOML, JSX, TSX, batch, and cmd.
- **Images**: Offers `.png` by default, plus `.jpg`, `.webp`, `.bmp`, `.gif`, `.tiff`, and `.ico`.
- **Audio**: Saves raw clipboard audio as `.wav` by default, with `.mp3`, `.flac`, `.ogg`, and `.aac` also available. If the clipboard contains a single copied audio file, PasteIt preserves its original format.
- **Video files**: Detects a single copied video file and saves it in its original format. PasteIt bundles `ffmpeg.exe` so video conversion works out of the box, and you can still override it in Settings or via `PATH` if needed.
- **URLs / links**: Saved as `.url` Windows Internet shortcuts.
- **HTML**: Saved as `.html`, with `.htm` and `.txt` also offered.

If the clipboard already contains regular files, PasteIt intentionally does nothing and lets normal file paste behavior remain unchanged.

## Features

- **Explorer context menu integration**: Adds a `Paste as File` menu for folder backgrounds and directories.
- **Explorer-aware hotkey**: `Ctrl+Shift+V` pastes into the currently focused Explorer folder.
- **Dynamic format options**: The menu changes based on the current clipboard contents.
- **History UI**: Stores paste history locally and shows previews for text-based entries plus file metadata for binary content.
- **Settings UI**: Configure history retention, filename prefix, default save location, and an optional FFmpeg override path.
- **Safe file naming**: Creates timestamped filenames and avoids overwriting existing files.
- **Toast notifications**: Shows success and error feedback after each paste.

## Solution Layout

The solution currently contains five projects:

- **`PasteIt.Core`**: Clipboard detection, language detection, extension resolution, file saving, history management, settings management, Explorer folder resolution, and startup registration.
- **`PasteIt`**: Background WinForms process that handles the `Ctrl+Shift+V` hotkey, executes paste operations, and registers or unregisters the shell extension.
- **`PasteItExtension`**: SharpShell-based Windows Explorer context menu extension.
- **`PasteIt.UI`**: WPF desktop app for viewing history and editing settings.
- **`PasteIt.Core.Tests`**: xUnit test project covering the core behavior.

## Installation

An Inno Setup installer is included.

1. Run `build_installer.ps1`.
2. The script builds `PasteIt.sln` in `Release` mode and then compiles `PasteIt.iss` with Inno Setup 6.
3. The installer is generated at `Output/PasteIt_Setup.exe`.
4. Run `Output/PasteIt_Setup.exe` as Administrator on a Windows x64 machine.

The installer copies the binaries, includes a bundled `ffmpeg.exe` plus its accompanying notice files, registers the shell extension, and launches `PasteIt.exe --service` once as the desktop user so startup registration happens under the correct user profile.

## Building And Testing

**Prerequisites**

- Windows x64
- .NET SDK that can build **.NET Framework 4.8** projects
- Visual Studio 2022 Build Tools or Visual Studio 2022 with the **.NET Framework 4.8 targeting pack**
- Inno Setup 6 if you want to build the installer

**Common commands**

```powershell
dotnet build .\PasteIt.sln -c Release
dotnet test .\PasteIt.Core.Tests\PasteIt.Core.Tests.csproj -c Release
```

## Manual Registration For Development

If you are running from build output instead of the installer, register the shell extension through the app:

```powershell
.\PasteIt\bin\Release\net48\PasteIt.exe --register-shell-extension ".\PasteItExtension\bin\Release\net48\PasteItExtension.dll"
```

To unregister it later:

```powershell
.\PasteIt\bin\Release\net48\PasteIt.exe --unregister-shell-extension ".\PasteItExtension\bin\Release\net48\PasteItExtension.dll"
```

## Data And Settings

PasteIt stores settings and history under `%AppData%\PasteIt` by default:

- `settings.json`
- `history.json`

For development or tests, the storage location can be overridden with the `PASTEIT_DATA_DIRECTORY` environment variable.

## Notes

- Video conversion uses the bundled `ffmpeg.exe` by default.
- If you set an FFmpeg path in Settings, that override takes precedence over the bundled copy.
- The default filename prefix is `clipboard`.
- If no explicit target directory is available, PasteIt falls back to the Desktop.

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on how to get started, report bugs, or suggest features.

## License

This project is licensed under the [MIT License](LICENSE).
