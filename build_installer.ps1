$ErrorActionPreference = "Stop"

Write-Host "Compiling PasteIt Solution (Release Mode)..." -ForegroundColor Cyan
dotnet build .\PasteIt.sln -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet build failed. Installer will not be built."
    exit 1
}

$InnoSetupCompiler = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

if (-not (Test-Path $InnoSetupCompiler)) {
    Write-Error "Inno Setup compiler (ISCC.exe) not found at $InnoSetupCompiler. Please install Inno Setup 6."
    exit 1
}

Write-Host ""
Write-Host "Building PasteIt_Setup.exe using Inno Setup..." -ForegroundColor Cyan
& $InnoSetupCompiler .\PasteIt.iss

if ($LASTEXITCODE -ne 0) {
    Write-Error "Inno Setup failed to build the installer."
    exit 1
}

Write-Host ""
Write-Host "SUCCESS! The installer has been created at:" -ForegroundColor Green
Write-Host "$PSScriptRoot\Output\PasteIt_Setup.exe" -ForegroundColor Green
