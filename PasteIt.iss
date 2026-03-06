#define ShellExtensionBundleVersion "2026.03.06.1"

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
Source: "PasteItExtension\bin\Release\net48\PasteItExtension.dll"; DestDir: "{app}"; Flags: ignoreversion; Check: ShouldInstallShellFiles
Source: "PasteItExtension\bin\Release\net48\SharpShell.dll"; DestDir: "{app}"; Flags: ignoreversion; Check: ShouldInstallShellFiles
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
Root: HKLM; Subkey: "Software\PasteIt"; ValueType: string; ValueName: "ShellExtensionBundleVersion"; ValueData: "{#ShellExtensionBundleVersion}"; Flags: uninsdeletevalue

[Run]
; Start PasteIt automatically after installation
Filename: "{app}\PasteIt.exe"; Parameters: "--service"; Flags: nowait runhidden

[UninstallRun]
; Kill service
Filename: "{cmd}"; Parameters: "/c taskkill /f /im PasteIt.exe"; Flags: runhidden
Filename: "{cmd}"; Parameters: "/c taskkill /f /im PasteIt.UI.exe"; Flags: runhidden
; Unregister shell extension
Filename: "{app}\PasteIt.exe"; Parameters: "--unregister-shell-extension ""{app}\PasteItExtension.dll"""; WorkingDir: "{app}"; Flags: runhidden waituntilterminated

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
var
  ExplorerRestartPending: Boolean;
  ShellRegistrationRequired: Boolean;
  ShellExplorerRestartRequired: Boolean;

procedure RunCmd(Params: String);
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{cmd}'), Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure ShowInstallStatus(Message: String);
begin
  if WizardForm <> nil then
  begin
    WizardForm.StatusLabel.Caption := Message;
    WizardForm.FilenameLabel.Caption := '';
    WizardForm.Update;
  end;
end;

procedure DetectShellUpgradeState(InstallDir: String);
var
  InstalledShellVersion: String;
  ExistingShellFilesPresent: Boolean;
begin
  ShellRegistrationRequired := True;
  ShellExplorerRestartRequired := False;

  ExistingShellFilesPresent :=
    FileExists(InstallDir + '\PasteItExtension.dll') or
    FileExists(InstallDir + '\SharpShell.dll');

  if not ExistingShellFilesPresent then
  begin
    exit;
  end;

  if RegQueryStringValue(HKLM, 'Software\PasteIt', 'ShellExtensionBundleVersion', InstalledShellVersion) and
     (InstalledShellVersion = '{#ShellExtensionBundleVersion}') then
  begin
    ShellRegistrationRequired := False;
    exit;
  end;

  ShellExplorerRestartRequired := True;
end;

function IsExplorerRunning(): Boolean;
var
  ResultCode: Integer;
  TempFile: String;
  Output: AnsiString;
begin
  TempFile := ExpandConstant('{tmp}\pasteit_explorer_state.txt');
  DeleteFile(TempFile);

  if not Exec(
    ExpandConstant('{cmd}'),
    '/c tasklist /FI "IMAGENAME eq explorer.exe" /NH > "' + TempFile + '"',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode) then
  begin
    Result := False;
    exit;
  end;

  Output := '';
  if not LoadStringFromFile(TempFile, Output) then
  begin
    Result := False;
    exit;
  end;

  Result := Pos('explorer.exe', Lowercase(String(Output))) > 0;
  DeleteFile(TempFile);
end;

procedure StopExplorerForUpgrade();
begin
  if not ShellExplorerRestartRequired then
  begin
    ExplorerRestartPending := False;
    exit;
  end;

  if not IsExplorerRunning() then
  begin
    ExplorerRestartPending := False;
    exit;
  end;

  ShowInstallStatus('Restarting Windows Explorer. Please be patient...');

  { Ask Explorer to exit cleanly first so the shell disappears and comes back gracefully. }
  RunCmd('/c taskkill /im explorer.exe >nul 2>nul');
  Sleep(2000);

  { If Explorer is still hanging on to the extension, fall back to a forced stop. }
  if IsExplorerRunning() then
  begin
    RunCmd('/c taskkill /f /im explorer.exe >nul 2>nul');
    Sleep(1500);
  end;

  ExplorerRestartPending := not IsExplorerRunning();
end;

function ShouldInstallShellFiles(): Boolean;
begin
  Result := ShellRegistrationRequired;
end;

function RunPasteIt(Parameters: String): Boolean;
var
  ResultCode: Integer;
begin
  if not Exec(
    ExpandConstant('{app}\PasteIt.exe'),
    Parameters,
    ExpandConstant('{app}'),
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode) then
  begin
    Result := False;
    exit;
  end;

  Result := ResultCode = 0;
end;

procedure StartExplorerIfNeeded();
var
  ResultCode: Integer;
begin
  if not ExplorerRestartPending then
  begin
    exit;
  end;

  Exec(ExpandConstant('{win}\explorer.exe'), '', '', SW_SHOW, ewNoWait, ResultCode);
  ExplorerRestartPending := False;
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
  ExplorerRestartPending := False;
  ShellRegistrationRequired := True;
  ShellExplorerRestartRequired := False;

  { 1. Kill PasteIt processes }
  RunCmd('/c taskkill /f /im PasteIt.exe 2>nul');
  RunCmd('/c taskkill /f /im PasteIt.UI.exe 2>nul');
  Sleep(500);

  InstallDir := ExpandConstant('{app}');
  DetectShellUpgradeState(InstallDir);

  { 2. Unregister the shell extension if the shell bundle changed }
  DllPath := InstallDir + '\PasteItExtension.dll';
  RegAsm := ExpandConstant('{win}') + '\Microsoft.NET\Framework64\v4.0.30319\regasm.exe';

  if ShellExplorerRestartRequired and FileExists(DllPath) and FileExists(RegAsm) then
  begin
    RunCmd('/c "' + RegAsm + '" "' + DllPath + '" /unregister 2>nul');
    Sleep(1000);
  end;

  { 3. Restart Explorer only when the shell bundle changed. }
  StopExplorerForUpgrade();

  if ShellExplorerRestartRequired then
  begin
    ShowInstallStatus('Installing updated shell components...');
  end
  else
  begin
    ShowInstallStatus('Installing updated application files...');
  end;

  { 4. Rename any locked files out of the way.                       }
  {    Explorer may still hold DLLs loaded in memory even after       }
  {    unregister. Windows allows renaming in-use files, so the new   }
  {    copies can be installed alongside. Explorer will load the new   }
  {    DLLs next time it loads the extension.                         }
  if ShellRegistrationRequired then
  begin
    MoveLockedFile(InstallDir + '\PasteItExtension.dll');
    MoveLockedFile(InstallDir + '\SharpShell.dll');
  end;
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

    if ShellRegistrationRequired then
    begin
      if ShellExplorerRestartRequired then
      begin
        ShowInstallStatus('Starting Windows Explorer again...');
      end
      else
      begin
        ShowInstallStatus('Registering shell extension...');
      end;

      RunPasteIt('--register-shell-extension "' + InstallDir + '\PasteItExtension.dll"');
      StartExplorerIfNeeded();
    end;
  end;
end;

procedure DeinitializeSetup();
begin
  StartExplorerIfNeeded();
end;
