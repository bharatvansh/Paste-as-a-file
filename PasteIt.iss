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
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin

[Files]
; Main service
Source: "PasteIt\bin\Release\net48\PasteIt.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "PasteIt\bin\Release\net48\PasteIt.exe.config"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; UI application
Source: "PasteIt.UI\bin\Release\net48\PasteIt.UI.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "PasteIt.UI\bin\Release\net48\PasteIt.UI.exe.config"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; Shell extension
Source: "PasteItExtension\bin\Release\net48\PasteItExtension.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "PasteItExtension\bin\Release\net48\SharpShell.dll"; DestDir: "{app}"; Flags: ignoreversion
; Core library
Source: "PasteItExtension\bin\Release\net48\PasteIt.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
; Any other DLLs
Source: "PasteIt\bin\Release\net48\*.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "PasteIt.UI\bin\Release\net48\*.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\PasteIt"; Filename: "{app}\PasteIt.UI.exe"; Comment: "Open PasteIt History & Settings"
Name: "{group}\Uninstall PasteIt"; Filename: "{uninstallexe}"

[Registry]
; Auto-start the background service on login
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "PasteItService"; ValueData: """{app}\PasteIt.exe"" --service"; Flags: uninsdeletevalue

[Run]
; Register shell extension
Filename: "{win}\Microsoft.NET\Framework64\v4.0.30319\regasm.exe"; Parameters: """{app}\PasteItExtension.dll"" /codebase"; WorkingDir: "{app}"; Flags: runhidden; StatusMsg: "Registering Shell Extension..."
; Start the background service
Filename: "{app}\PasteIt.exe"; Parameters: "--service"; Description: "Launch PasteIt background service"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Kill service
Filename: "{cmd}"; Parameters: "/c taskkill /f /im PasteIt.exe"; Flags: runhidden
Filename: "{cmd}"; Parameters: "/c taskkill /f /im PasteIt.UI.exe"; Flags: runhidden
; Unregister shell extension
Filename: "{win}\Microsoft.NET\Framework64\v4.0.30319\regasm.exe"; Parameters: """{app}\PasteItExtension.dll"" /unregister"; WorkingDir: "{app}"; Flags: runhidden

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
