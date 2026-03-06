# PasteIt

**PasteIt** is a Windows utility that allows you to paste the content of your clipboard directly as a new file in Windows Explorer. Instead of manually creating a file, opening it, pasting the content, and saving it, you can simply right-click in any directory and choose "Paste as File".

PasteIt currently targets **Windows x64** systems.

PasteIt intelligently detects the clipboard content type:
- **Plain Text**: Saved as `.txt` files.
- **Code snippets**: The custom-built language detector identifies nearly 20 languages (Python, JavaScript, TypeScript, C#, Java, C++, C, HTML, CSS, XML, SQL, Go, Rust, Kotlin, Swift, PHP, Ruby, Shell scripts, PowerShell, and JSON) and automatically applies the correct file extension (e.g., `.js`, `.py`, `.sql`).
- **Images**: Offers `.png` by default, plus `.jpg`, `.webp`, `.bmp`, `.gif`, `.tiff`, and `.ico`.
- **Video files**: Detects a single copied video file from the clipboard and saves it in its original format. When `ffmpeg` is available on `PATH` or configured in Settings, it can also convert between common formats like `.mp4`, `.mov`, `.avi`, `.mkv`, `.webm`, `.wmv`, `.m4v`, and `.mpeg`.
- **URLs / Links**: Saved as `.url` Windows Internet shortcuts.
- **HTML data**: Saved as `.html` pages.

## Features

- **Context Menu Integration**: Right-click in any folder or on any folder background in Windows Explorer to find the "Paste as File" menu and its dynamic format options for the detected clipboard content.
- **Smart Content Detection**: Parses the Windows Clipboard prioritizing the exact content the user actually intended (differentiating plain text from rich text, handling code, finding images, etc).
- **History Tracking**: Optionally keeps a searchable local history of your pasted items, providing previews for text/code and recording saved file locations.
- **Silent Operation**: Operates quickly through a background CLI executable that parses the request, dumps the file, raises a sleek Windows Toast Notification, and exits. 
- **Settings & History UI**: View paste history and manage application settings through the robust WPF-based UI.

## Architecture

The project is built on the .NET Framework 4.8 and is divided into four main projects:

- **`PasteIt.Core`**: The shared logic library. Contains the core engine logic for evaluating clipboard content (`ClipboardDetector`), language detection (`CodeLanguageDetector`), saving files intelligently without overwriting others (`FileSaver`), and parsing the current Windows Explorer COM objects to locate the active working directory (`ExplorerHelper`).
- **`PasteIt` (Service / CLI)**: A hidden background executor. Depending on its arguments, it will either run continuously as a service (listening for hotkeys via a hook) or execute a single fast paste operation triggered by the Windows Explorer shell.
- **`PasteItExtension`**: A Windows Shell Extension built over SharpShell. It resolves the user's right-click context (whether selecting an empty space or a directory icon) and triggers the core `PasteIt` CLI with the correct underlying folder path.
- **`PasteIt.UI`**: A WPF client designed for viewing your paste history and modifying the application's configuration.

## Installation

An automated installer approach is provided out of the box leveraging [Inno Setup](https://jrsoftware.org/isinfo.php).

1. Execute the `build_installer.ps1` PowerShell script.
2. The script compiles the entire `.sln` solution via `dotnet build` and delegates packaging to Inno Setup 6 using the `PasteIt.iss` script.
3. Wait for the build to finish. The generated installer will be located in the `Output/` directory as `PasteIt_Setup.exe`.
4. Run `PasteIt_Setup.exe` as an administrator on a Windows x64 machine. The installer copies the application files, registers the shell extension, and launches PasteIt once under your desktop user account so it can configure per-user startup settings.

Paste history is stored locally under your user profile when history is enabled in Settings. You can disable history or startup-on-login from the built-in UI at any time.

## Building manually

**Prerequisites:**
- Windows x64
- .NET SDK with support for building **.NET Framework 4.8** projects
- Visual Studio 2022 Build Tools or Visual Studio 2022 with the **.NET Framework 4.8** targeting pack
- Inno Setup 6 (if you want to package the installer)

1. Clone or download the repository.
2. Build the solution with `dotnet build PasteIt.sln -c Release` or from Visual Studio.
3. If debugging the shell extension layer, manually register `PasteItExtension.dll` for COM interop using the 64-bit `regasm.exe /codebase`.
