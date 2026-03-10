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

[Files]
; Main service
Source: "PasteIt\bin\Release\net48\PasteIt.exe"; DestDir: "{app}"; Flags: ignoreversion restartreplace
Source: "PasteIt\bin\Release\net48\PasteIt.exe.config"; DestDir: "{app}"; Flags: ignoreversion restartreplace skipifsourcedoesntexist
; UI application
Source: "PasteIt.UI\bin\Release\net48\PasteIt.UI.exe"; DestDir: "{app}"; Flags: ignoreversion restartreplace
Source: "PasteIt.UI\bin\Release\net48\PasteIt.UI.exe.config"; DestDir: "{app}"; Flags: ignoreversion restartreplace skipifsourcedoesntexist
; Shell extension
Source: "PasteItExtension\bin\Release\net48\PasteItExtension.dll"; DestDir: "{app}"; Flags: ignoreversion restartreplace
Source: "PasteItExtension\bin\Release\net48\SharpShell.dll"; DestDir: "{app}"; Flags: ignoreversion restartreplace
; Core library
Source: "PasteItExtension\bin\Release\net48\PasteIt.Core.dll"; DestDir: "{app}"; Flags: ignoreversion restartreplace
; Bundled FFmpeg
Source: "ThirdParty\FFmpeg\ffmpeg.exe"; DestDir: "{app}"; Flags: ignoreversion restartreplace skipifsourcedoesntexist
Source: "ThirdParty\FFmpeg\LICENSE.txt"; DestDir: "{app}"; DestName: "ffmpeg-LICENSE.txt"; Flags: ignoreversion restartreplace skipifsourcedoesntexist
Source: "ThirdParty\FFmpeg\README.txt"; DestDir: "{app}"; DestName: "ffmpeg-README.txt"; Flags: ignoreversion restartreplace skipifsourcedoesntexist
; Any other DLLs
Source: "PasteIt\bin\Release\net48\*.dll"; DestDir: "{app}"; Flags: ignoreversion restartreplace skipifsourcedoesntexist; Excludes: "PasteIt.Core.dll,SharpShell.dll"

[Icons]
Name: "{group}\PasteIt"; Filename: "{app}\PasteIt.UI.exe"; Comment: "Open PasteIt History & Settings"
Name: "{group}\Uninstall PasteIt"; Filename: "{uninstallexe}"

[Run]
; Launch once as the installing desktop user so runtime registration happens in the correct HKCU profile.
Filename: "{app}\PasteIt.exe"; Parameters: "--service"; Flags: nowait runasoriginaluser runhidden skipifsilent

[UninstallRun]
; Kill service
Filename: "{cmd}"; Parameters: "/c taskkill /f /im PasteIt.exe"; Flags: runhidden; RunOnceId: "KillPasteItExe"
Filename: "{cmd}"; Parameters: "/c taskkill /f /im PasteIt.UI.exe"; Flags: runhidden; RunOnceId: "KillPasteItUiExe"
; Unregister shell extension
Filename: "{app}\PasteIt.exe"; Parameters: "--unregister-shell-extension ""{app}\PasteItExtension.dll"""; WorkingDir: "{app}"; Flags: runhidden waituntilterminated; RunOnceId: "UnregisterPasteItShellExtension"

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
begin
  ShellRegistrationRequired := True;
  ShellExplorerRestartRequired :=
    FileExists(InstallDir + '\PasteItExtension.dll') or
    FileExists(InstallDir + '\SharpShell.dll') or
    FileExists(InstallDir + '\PasteIt.Core.dll');
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

      if not RunPasteIt('--register-shell-extension "' + InstallDir + '\PasteItExtension.dll"') then
      begin
        StartExplorerIfNeeded();
        RaiseException(
          'PasteIt setup could not register the Windows Explorer shell extension.' + #13#10#13#10 +
          'The install is incomplete. Please rerun the installer as Administrator.');
      end;

      StartExplorerIfNeeded();
    end;
  end;
end;

procedure DeinitializeSetup();
begin
  StartExplorerIfNeeded();
end;
