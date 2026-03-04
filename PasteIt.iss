[Setup]
AppName=PasteIt
AppVersion=1.0.0
AppPublisher=PasteIt Open Source
DefaultDirName={autopf}\PasteIt
DefaultGroupName=PasteIt
UninstallDisplayIcon={app}\PasteIt.exe
Compression=lzma2
SolidCompression=yes
OutputDir=Output
OutputBaseFilename=PasteIt_Setup
; "ArchitecturesInstallIn64BitMode=x64" ensures it installs to "Program Files" not "Program Files (x86)" on 64-bit Windows
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
ChangesEnvironment=yes

[Files]
; Explicitly copy the exe, the shell extension dll, and SharpShell.dll
Source: "PasteIt\bin\Release\net48\PasteIt.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "PasteIt\bin\Release\net48\PasteIt.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "PasteItExtension\bin\Release\net48\PasteItExtension.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "PasteItExtension\bin\Release\net48\SharpShell.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "PasteItExtension\bin\Release\net48\PasteIt.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
; Also copy any other referenced DLLs in the output folder just to be safe
Source: "PasteIt\bin\Release\net48\*.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Registry]
; Add the auto-start entry
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "PasteItService"; ValueData: """{app}\PasteIt.exe"" --service"; Flags: uninsdeletevalue

[Run]
; Regasm the shell extension during install
Filename: "{win}\Microsoft.NET\Framework64\v4.0.30319\regasm.exe"; Parameters: """{app}\PasteItExtension.dll"" /codebase"; WorkingDir: "{app}"; Flags: runhidden; StatusMsg: "Registering Shell Extension..."
; Start the service right away
Filename: "{app}\PasteIt.exe"; Parameters: "--service"; Description: "Launch PasteIt background service"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Close the service if running
Filename: "{cmd}"; Parameters: "/c taskkill /f /im PasteIt.exe"; Flags: runhidden
; Unregister the shell extension during uninstall
Filename: "{win}\Microsoft.NET\Framework64\v4.0.30319\regasm.exe"; Parameters: """{app}\PasteItExtension.dll"" /unregister"; WorkingDir: "{app}"; Flags: runhidden

[UninstallDelete]
; Clean up the entire app directory just in case it leaves log files or old dlls
Type: filesandordirs; Name: "{app}"
