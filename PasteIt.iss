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
DirExistsWarning=no
SetupIconFile=PasteIt.Core\Resources\logo.ico
CloseApplications=no

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

[Code]
procedure RunCmd(Params: String);
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{cmd}'), Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

{ Rename a locked file out of the way so a new copy can be installed. }
{ Windows allows renaming memory-mapped / in-use files.               }
procedure MoveLockedFile(FilePath: String);
var
  OldPath: String;
begin
  if not FileExists(FilePath) then Exit;

  { Try to delete first — if it works, no rename needed }
  if DeleteFile(FilePath) then Exit;

  { File is locked. Rename it so Inno Setup can write the new one. }
  OldPath := FilePath + '.old';
  if FileExists(OldPath) then
    DeleteFile(OldPath);
  RenameFile(FilePath, OldPath);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  InstallDir: String;
  RegAsm: String;
  DllPath: String;
begin
  Result := '';
  NeedsRestart := False;

  { 1. Kill PasteIt processes }
  RunCmd('/c taskkill /f /im PasteIt.exe 2>nul');
  RunCmd('/c taskkill /f /im PasteIt.UI.exe 2>nul');
  Sleep(500);

  { 2. Unregister the shell extension }
  InstallDir := ExpandConstant('{app}');
  DllPath := InstallDir + '\PasteItExtension.dll';
  RegAsm := ExpandConstant('{win}') + '\Microsoft.NET\Framework64\v4.0.30319\regasm.exe';

  if FileExists(DllPath) and FileExists(RegAsm) then
  begin
    RunCmd('/c "' + RegAsm + '" "' + DllPath + '" /unregister 2>nul');
    Sleep(1000);
  end;

  { 3. Rename any locked files out of the way.                       }
  {    Explorer may still hold DLLs loaded in memory even after       }
  {    unregister. Windows allows renaming in-use files, so the new   }
  {    copies can be installed alongside. Explorer will load the new   }
  {    DLLs next time it loads the extension.                         }
  MoveLockedFile(InstallDir + '\PasteItExtension.dll');
  MoveLockedFile(InstallDir + '\SharpShell.dll');
  MoveLockedFile(InstallDir + '\PasteIt.Core.dll');
  MoveLockedFile(InstallDir + '\PasteIt.exe');
  MoveLockedFile(InstallDir + '\PasteIt.UI.exe');
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  InstallDir: String;
begin
  if CurStep = ssPostInstall then
  begin
    { Clean up any .old files left from the rename trick }
    InstallDir := ExpandConstant('{app}');
    DeleteFile(InstallDir + '\PasteItExtension.dll.old');
    DeleteFile(InstallDir + '\SharpShell.dll.old');
    DeleteFile(InstallDir + '\PasteIt.Core.dll.old');
    DeleteFile(InstallDir + '\PasteIt.exe.old');
    DeleteFile(InstallDir + '\PasteIt.UI.exe.old');
  end;
end;
