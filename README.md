# PasteIt

PasteIt is a Windows clipboard utility that saves clipboard content directly as a file in the current Explorer folder.

## What It Does

- Adds an Explorer context menu item: `Paste as File`
- Runs as a background hotkey service (`Ctrl + Shift + V`)
- Detects clipboard content type and saves using the right file extension
- Creates smart filenames like `clipboard_2026-02-14_001.py`
- Shows a confirmation notification after save

## Project Structure

- `PasteIt.Core`: clipboard detection, language heuristics, file save logic, Explorer helpers
- `PasteIt`: background app with hotkey listener and one-shot `--paste` mode
- `PasteItExtension`: SharpShell context-menu extension that invokes `PasteIt.exe`
- `PasteIt.Core.Tests`: unit tests for code detection and file save behavior

## Detection Rules

- Image clipboard (`CF_BITMAP` / `CF_DIB`) -> `.png`
- URL text -> `.url`
- HTML clipboard (`CF_HTML`) -> `.html`
- Code text -> detected extension (`.py`, `.js`, `.ts`, `.cs`, `.java`, `.cpp`, `.c`, `.json`, `.sql`, `.go`, `.rs`, `.kt`, `.swift`, `.php`, `.rb`, `.sh`, `.ps1`, `.xml`, `.html`, `.css`)
- Plain text -> `.txt`
- File drop list (`CF_HDROP`) -> ignored (already files)

## Build (Windows)

Use Visual Studio 2022 or MSBuild on Windows with .NET Framework 4.8 developer tools installed.

```powershell
msbuild .\PasteIt.sln /p:Configuration=Release
```

## Run

One-shot paste:

```powershell
.\PasteIt\bin\Release\PasteIt.exe --paste --target "C:\Users\<you>\Desktop"
```

Service mode (hotkey listener):

```powershell
.\PasteIt\bin\Release\PasteIt.exe --service
```

## Register Shell Extension

Build `PasteItExtension.dll`, then register with `regasm` (run as admin if registering to HKLM):

```powershell
& "$env:windir\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" `
  ".\PasteItExtension\bin\Release\PasteItExtension.dll" /codebase
```

Optional registry values for extension path lookup:

- `HKCU\Software\PasteIt\ExecutablePath` = full path to `PasteIt.exe`
- or `HKCU\Software\PasteIt\InstallPath` = folder containing `PasteIt.exe`
- `PasteIt.exe` also updates these values automatically on startup (best effort).

## Tests

```powershell
dotnet test .\PasteIt.Core.Tests\PasteIt.Core.Tests.csproj
```

## Notes

- Hotkey trigger runs only when Explorer is the foreground window (`CabinetWClass`).
- If no Explorer folder can be resolved, save target falls back to Desktop.
