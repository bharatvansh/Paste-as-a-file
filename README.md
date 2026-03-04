# PasteIt

PasteIt is an open-source Windows clipboard utility that saves the current contents of your clipboard directly as a file in the folder you are currently looking at.

## Features

- **Explorer Integration:** Adds an instant `Paste as File` item to the Windows Right-Click Context Menu.
- **Global Hotkey:** Runs silently in the background, listening for `Ctrl + Shift + V`.
- **Smart Automatic Detection:** Detects the clipboard content type and saves using the correct file extension.
  - Image payloads (`CF_BITMAP` / `CF_DIB`) become `.png` images.
  - Formatted Web Links become `.url` shortcuts.
  - Code text intelligently guesses the language (`.py`, `.js`, `.ts`, `.cs`, `.java`, `.json`, `.sql`, etc.).
  - Plain text defaults to `.txt` files.
- **Smart Naming:** Generates automatic filenames like `clipboard_2026-02-14_001.py`.
- **Notifications:** Brief Windows confirmation pop-ups ensure you know the file was successfully saved.

---

## 🚀 Easy Installation (For Users)

The easiest way to install PasteIt is using our pre-built setup file.

1. Download or generate the `PasteIt_Setup.exe` installer.
2. Double-click the installer and follow the prompt.
3. The installer will automatically:
   - Install the required files to `C:\Program Files\PasteIt`.
   - Register the Windows Explorer Right-Click Menu.
   - Configure the background global hotkey to start automatically when you boot Windows.
   - Launch the application so you can start pasting immediately!

*To completely remove PasteIt, simply navigate to "Add or Remove Programs" in your Windows Settings and choose Uninstall.*

---

## 🛠️ Building from Source (For Developers)

Contributions, issues, and feature requests are welcome! Feel free to check the [issues page]. If you want to contribute to the tool or compile it yourself, here is how.

### Project Structure

- `PasteIt.Core`: The core logic for clipboard detection, programming language heuristics, and file saving.
- `PasteIt`: The main executable (`.exe`) that listens as a background service or accepts one-shot `--paste` commands.
- `PasteItExtension`: A `SharpShell` COM library (`.dll`) that hooks into the Windows Context Menu.
- `PasteIt.Core.Tests`: The unit tests for code detection and file save behavior.

### Requirements
- Windows OS
- .NET Framework 4.8 Developer Pack
- MSBuild / .NET CLI (or Visual Studio 2022)
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) (Only required if you want to generate the Installer)

### Compiling the Solution

You can easily build the `PasteIt.sln` solution via the command line:

```powershell
dotnet build .\PasteIt.sln -c Release
```

### Generating the Installer

We have provided an automated script that compiles the code and generates the final `PasteIt_Setup.exe` inside an `/Output` folder:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\build_installer.ps1
```

### Manual Testing

You can run the executable in one-shot paste mode to test logic without starting the listener:

```powershell
.\PasteIt\bin\Release\net48\PasteIt.exe --paste --target "C:\Users\<you>\Desktop"
```

Run the unit tests using:
```powershell
dotnet test .\PasteIt.Core.Tests\PasteIt.Core.Tests.csproj
```

### Manual Shell Extension Registration

If you don't use the installer and want to test the right-click menu during development, build `PasteItExtension.dll`, then register with `regasm` (run an Administrator PowerShell):

```powershell
& "$env:windir\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" `
  ".\PasteItExtension\bin\Release\net48\PasteItExtension.dll" /codebase
```

## License

This project is licensed under the MIT License - see the `LICENSE` file for details.
